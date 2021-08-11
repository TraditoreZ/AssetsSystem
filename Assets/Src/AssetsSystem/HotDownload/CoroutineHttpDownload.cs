using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CoroutineHttpDownload : MonoBehaviour
{

    const int ThreadContMax = 16;

    private static CoroutineHttpDownload _instance;

    public static CoroutineHttpDownload instance
    {
        get
        {
            if (_instance == null)
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 128;
                GameObject go = new GameObject("HttpDownload");
                _instance = go.AddComponent<CoroutineHttpDownload>();
                GameObject.DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    public long downloadTotalLength { get; private set; }
    public long downLoadMaxSize { get; private set; }
    private Queue<DownloadReqData> tasks = new Queue<DownloadReqData>();
    private Action<bool> resultCallBack;
    private Action<long, long, long> processAndSpeed;
    int threadStard = 0;
    // ###下载速度计算
    private long lastDownloadPreSecond;
    private float downLoadDeltaTime;
    private long downLoadSpeedPreSecond;
    // ### 下载速度计算部分

    public void DownloadFile(DownloadReqData[] req, Action<bool> resultCallBack, Action<long, long, long> processAndSpeed = null)
    {
        this.resultCallBack = resultCallBack;
        this.processAndSpeed = processAndSpeed;
        foreach (var item in req)
        {
            tasks.Enqueue(item);
            downLoadMaxSize += item.size;
        }

        if (threadStard < ThreadContMax)
        {
            int needStartThread = ThreadContMax - threadStard;
            for (int i = 0; i < needStartThread; i++)
            {
                StartCoroutine(DownloadThread());
                threadStard++;
            }
        }
    }


    private IEnumerator DownloadThread()
    {
        while (tasks.Count > 0)
        {
            DownloadReqData data = tasks.Dequeue();
            yield return DownloadUnit(data);
        }
        threadStard--;
        if (threadStard == 0)
        {
            resultCallBack?.Invoke(true);
            downloadTotalLength = 0;
            lastDownloadPreSecond = 0;
            downLoadDeltaTime = 0;
            downLoadSpeedPreSecond = 0;
            downLoadMaxSize = 0;
        }
    }


    private IEnumerator DownloadUnit(DownloadReqData data, int reConnectionCount = 3)
    {
        if (!Directory.Exists(Path.GetDirectoryName(data.filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(data.filePath));
        }
        UnityWebRequest www = new UnityWebRequest(data.url);
        DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
        www.disposeDownloadHandlerOnDispose = true;
        www.downloadHandler = dH;
        www.SendWebRequest();
        ulong lastDownloadbytes = 0;
        while (!www.isDone)
        {
            yield return null;
            long downloadBytes = (long)(www.downloadedBytes - lastDownloadbytes);
            lastDownloadbytes = www.downloadedBytes;
            downloadTotalLength += downloadBytes;
        }
        if (www.isHttpError || www.isNetworkError)
        {
            if (reConnectionCount > 0)
            {
                Debug.LogWarning(data.url + "  Download Faild. reConnectionCount:" + reConnectionCount);
                downloadTotalLength -= (long)www.downloadedBytes;
                yield return DownloadUnit(data, reConnectionCount - 1);
            }
            else
            {
                Error(www.error);
                yield break;
            }
        }
        using (FileStream fs = new FileStream(data.filePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            fs.Write(www.downloadHandler.data, 0, www.downloadHandler.data.Length);
        }
        www.Dispose();
    }


    private void Error(string error)
    {
        Debug.LogError(error);
        StopAllCoroutines();
        tasks.Clear();
        downloadTotalLength = 0;
        lastDownloadPreSecond = 0;
        downLoadDeltaTime = 0;
        downLoadSpeedPreSecond = 0;
        downLoadMaxSize = 0;
        resultCallBack?.Invoke(false);
    }


    void Update()
    {
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
            processAndSpeed?.Invoke(downloadTotalLength, downLoadMaxSize, downLoadSpeedPreSecond);
        }

    }

}