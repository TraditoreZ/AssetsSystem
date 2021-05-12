using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetSystem
{
    public class WWWHotDownload : IAssetHotDownload
    {

        public virtual bool CheckAssetVersion(string localVersion, string remoteVersion)
        {
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

        public virtual string GetLocalVersion()
        {
            return PlayerPrefs.GetString("localVersion");
        }

        public virtual void GetRemoteVersion(Action<string> result)
        {
            result?.Invoke("2");
        }

        public virtual void SetLocalVersion(string version)
        {
            PlayerPrefs.SetString("localVersion", version);
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
        }

    }
}