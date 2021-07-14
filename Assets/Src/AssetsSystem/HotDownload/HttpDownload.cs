using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;

public struct DownloadReqData
{
    public string url;
    public string filePath;
    public long size;
}

public class HttpDownload : MonoBehaviour
{
    private static object _lock = new object();

    private static HttpDownload _instance;

    public static HttpDownload instance
    {
        get
        {
            if (_instance == null)
            {

                System.Net.ServicePointManager.DefaultConnectionLimit = 50;
                GameObject go = new GameObject("HttpDownload");
                _instance = go.AddComponent<HttpDownload>();
                _instance.active = true;
                GameObject.DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public long downloadTotalLength { get; private set; }

    // ###下载速度计算
    private long lastDownloadPreSecond;
    private float downLoadDeltaTime;
    private long downLoadSpeedPreSecond;
    // ### 下载速度计算部分
    const int ThreadContMax = 8;
    const int ReadWriteTimeOut = 50000;
    const int TimeOutWait = 50000;
    const int bufferSize = 1048576;
    private ConcurrentQueue<DownloadReqData> tasks = new ConcurrentQueue<DownloadReqData>();
    int threadStard = 0;

    private Action<bool> resultCallBack;
    private Action<long, long> processAndSpeed;
    bool isError;

    bool downloading;

    bool active;
    public void DownloadFile(DownloadReqData[] req, Action<bool> resultCallBack, Action<long, long> processAndSpeed = null)
    {
        this.resultCallBack = resultCallBack;
        this.processAndSpeed = processAndSpeed;
        foreach (var item in req)
        {
            tasks.Enqueue(item);
        }
        if (threadStard < ThreadContMax)
        {
            int needStartThread = ThreadContMax - threadStard;
            for (int i = 0; i < needStartThread; i++)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(DownloadThread));
                thread.Start(thread);
                lock (_lock)
                {
                    threadStard++;
                    Debug.Log("HttpDownload Thread:" + threadStard);
                }
            }
        }
    }

    private void DownloadThread(object threadObj)
    {
        Thread thread = threadObj as Thread;
        while (tasks.Count > 0 && isError == false && active)
        {
            DownloadReqData data;
            if (tasks.TryDequeue(out data))
            {
                try
                {
                    lock (_lock)
                    {
                        downloading = true;
                    }
                    DownloadUnit(data);
                }
                catch (System.Exception e)
                {
                    lock (_lock)
                    {
                        isError = true;
                    }
                    Debug.LogError(data.url + "\r\n" + e);
                }
            }
        }
        lock (_lock)
        {
            threadStard--;
        }
        thread.Abort();
    }


    private void DownloadUnit(DownloadReqData data)
    {
        //判断保存路径是否存在
        if (!Directory.Exists(Path.GetDirectoryName(data.filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(data.filePath));
        }

        //使用流操作文件
        FileStream fs = new FileStream(data.filePath + ".download", FileMode.OpenOrCreate, FileAccess.Write);
        //获取文件现在的长度
        long fileLength = fs.Length;
        //获取下载文件的总长度
        long totalLength = GetLength(data.url);
        //Debug.LogFormat("<color=yellow>文件:{0} 已下载{1}kb，剩余{2}kb</color>", Path.GetFileName(data.filePath), fileLength / 1024, (totalLength - fileLength) / 1024);

        //如果没下载完
        if (fileLength < totalLength)
        {
            //断点续传核心，设置本地文件流的起始位置
            fs.Seek(fileLength, SeekOrigin.Begin);

            HttpWebRequest request = HttpWebRequest.Create(data.url) as HttpWebRequest;
            request.KeepAlive = false;
            request.ReadWriteTimeout = ReadWriteTimeOut;
            request.Timeout = TimeOutWait;

            //断点续传核心，设置远程访问文件流的起始位置
            request.AddRange((int)fileLength);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            byte[] buffer = new byte[bufferSize];
            //使用流读取内容到buffer中
            //注意方法返回值代表读取的实际长度,并不是buffer有多大，stream就会读进去多少
            int length = stream.Read(buffer, 0, buffer.Length);
            //Debug.LogFormat("<color=red>length:{0}</color>" + length);
            while (length > 0)
            {
                //将内容再写入本地文件中
                fs.Write(buffer, 0, length);
                //计算进度
                fileLength += length;
                {
                    lock (_lock)
                    {
                        downloadTotalLength += length;
                    }
                }
                //progress = (float)fileLength / (float)totalLength;
                //UnityEngine.Debug.Log(progress);
                //类似尾递归
                length = stream.Read(buffer, 0, buffer.Length);

            }
            stream.Close();
            stream.Dispose();
            request.Abort();
            response.Close();
            response.Dispose();
        }
        else
        {
            lock (_lock)
            {
                downloadTotalLength += fileLength;
            }
            //progress = 1;
        }
        fs.Close();
        fs.Dispose();
        File.Move(data.filePath + ".download", data.filePath);
    }


    /// <summary>
    /// 获取下载文件的大小
    /// </summary>
    /// <returns>The length.</returns>
    /// <param name="url">URL.</param>
    long GetLength(string url)
    {
        long len = 0;
        HttpWebRequest requet = HttpWebRequest.Create(url) as HttpWebRequest;
        requet.KeepAlive = false;
        requet.Method = "HEAD";
        HttpWebResponse response = requet.GetResponse() as HttpWebResponse;
        len = response.ContentLength;
        requet.Abort();
        response.Close();
        response.Dispose();
        return len;
    }

    void Update()
    {
        if (downloading && threadStard == 0 && tasks.Count == 0)
        {
            lock (_lock)
            {
                Debug.Log("HttpDownload Over");
                bool error = isError;
                downloadTotalLength = 0;
                lastDownloadPreSecond = 0;
                downLoadDeltaTime = 0;
                isError = false;
                downloading = false;
                resultCallBack?.Invoke(error == false);
            }
        }

        //下载速度计算
        if (threadStard > 0)
        {
            if (downLoadDeltaTime >= 1)
            {
                downLoadDeltaTime -= 1;
                downLoadSpeedPreSecond = downloadTotalLength - lastDownloadPreSecond;
                lastDownloadPreSecond = downloadTotalLength;
            }
            downLoadDeltaTime += Time.deltaTime;
            processAndSpeed?.Invoke(downloadTotalLength, downLoadSpeedPreSecond);
        }

    }

    void Destory()
    {
        active = false;
    }

}