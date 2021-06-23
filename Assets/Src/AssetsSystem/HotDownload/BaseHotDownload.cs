using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetSystem
{
    public class BaseHotDownload : IAssetHotDownload
    {
        private const int mathSpeedCount = 30;
        private const int reConnectionCount = 3;
        private Queue<float> downLoadSpeeds = new Queue<float>();

        public virtual bool CheckPersistentResource()
        {
            return true;
        }

        public virtual void CheckRemoteVersion(string localVersion, string remoteVersion, Action<bool> updateCall)
        {
            Debug.Log("CheckAssetVersion    " + "local:" + localVersion + "  remote:" + remoteVersion);
            // 这里做成异步， 非强更类型可以弹出提示框让玩家选择更新
            updateCall.Invoke(!localVersion.Equals(remoteVersion));
        }

        public virtual void Download(string url, Action<long> process, Action<bool, byte[]> resultCallBack)
        {
            AssetDownload.instance.StartCoroutine(LoadResourceCorotine(url, (bytes) =>
            {
                resultCallBack?.Invoke(true, bytes);
            }, (error) =>
            {
                resultCallBack?.Invoke(false, null);
            }, (byteSize) =>
            {
                process?.Invoke((long)byteSize);
            }, reConnectionCount));
        }

        public float DownloadSpeed()
        {
            float allSpeed = 0;
            foreach (var speed in downLoadSpeeds)
            {
                allSpeed += speed;
            }
            return allSpeed / downLoadSpeeds.Count;
        }

        public virtual void GetLocalVersion(Action<string> result)
        {
            string url = AssetBundlePathResolver.instance.GetBundleFileRuntime("version.txt", true);
            Download(url, null, (ok, bytes) =>
           {
               if (ok)
               {
                   string localVer = System.Text.Encoding.Default.GetString(bytes);
                   result?.Invoke(localVer);
               }
               else
               {
                   throw new Exception("GetLocalVersion Fail");
               }
           });
        }

        public virtual void GetRemoteVersion(Action<string> result)
        {
            string url = string.Format("{0}/{1}/version.txt", AssetDownload.instance.remoteUrl, AssetBundlePathResolver.instance.GetBundlePlatformRuntime());
            Download(url, null, (ok, bytes) =>
            {
                if (ok)
                {
                    string remoteVer = System.Text.Encoding.Default.GetString(bytes);
                    result?.Invoke(remoteVer);
                }
                else
                {
                    throw new Exception("GetRemoteVersion Fail");
                }
            });
        }

        public virtual void SetLocalVersion(string version)
        {
            string localPath = AssetBundlePathResolver.instance.GetBundlePersistentFile("version.txt");
            HDResolver.WriteFile(localPath, version);
        }

        private IEnumerator LoadResourceCorotine(string url, Action<byte[]> downloadOverCall, Action<string> errorCall, Action<ulong> downLoadBytes, int reConnectionCount)
        {
            UnityWebRequest www = new UnityWebRequest(url);
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
                GetDownloadSpeed(downloadBytes);
                downLoadBytes?.Invoke(www.downloadedBytes);
            }
            if (www.isHttpError || www.isNetworkError)
            {
                if (reConnectionCount > 0)
                {
                    Debug.LogWarning("www is error try reConnection :" + reConnectionCount);
                    yield return LoadResourceCorotine(url, downloadOverCall, errorCall, downLoadBytes, reConnectionCount - 1);
                }
                else
                {
                    Debug.LogError(www.error);
                    errorCall?.Invoke(www.error);
                    yield break;
                }
            }
            if (www.isDone)
            {
                downloadOverCall?.Invoke(www.downloadHandler.data);
            }
            www.Dispose();
        }

        private void GetDownloadSpeed(long downLoadbytes)
        {
            downLoadSpeeds.Enqueue(downLoadbytes / Time.deltaTime);
            if (downLoadSpeeds.Count > mathSpeedCount)
            {
                downLoadSpeeds.Dequeue();
            }
        }

        public bool IsFirstRun()
        {
            string localPath = AssetBundlePathResolver.instance.GetBundlePersistentFile("version.txt");
            return System.IO.Directory.GetFiles(AssetBundlePathResolver.instance.GetBundlePersistentFile()).Length == 0
            && System.IO.Directory.GetDirectories(AssetBundlePathResolver.instance.GetBundlePersistentFile()).Length == 0;
        }
    }
}