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
        private string localVer = "0";

        public void Download(string url, Action<long> process, Action<bool, byte[]> resultCallBack)
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

        public string GetLocalVersion()
        {
            return localVer;
        }

        public void GetRemoteVersion(Action<string> result)
        {
            result?.Invoke("1");
        }

        public void SetLocalVersion(string version)
        {
            localVer = version;
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