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
        public virtual bool CheckPersistentResource()
        {
            return true;
        }

        public virtual bool CheckRemoteVersion(string localVersion, string remoteVersion)
        {
            Debug.Log("CheckAssetVersion    " + "local:" + localVersion + "  remote:" + remoteVersion);
            return localVersion.Equals(remoteVersion);
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
            }));
        }

        public virtual void GetLocalVersion(Action<string> result)
        {
            string url = AssetBundlePathResolver.instance.GetBundleFileRuntime("version.txt");
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

        private IEnumerator LoadResourceCorotine(string url, Action<byte[]> downloadOverCall, Action<string> errorCall, Action<ulong> downLoadBytes)
        {
            UnityWebRequest www = new UnityWebRequest(url);
            DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
            www.downloadHandler = dH;
            www.SendWebRequest();
            while (!www.isDone)
            {
                yield return null;
                downLoadBytes?.Invoke(www.downloadedBytes);
            }
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.LogError(www.error);
                errorCall?.Invoke(www.error);
                yield break;
            }
            if (www.isDone)
            {
                downloadOverCall?.Invoke(www.downloadHandler.data);
            }
            www.Dispose();
        }

    }
}