using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace AssetSystem
{
    public interface IAssetHotDownload
    {
        // 回调参数 第一个为最新版本信息， 第二个为版本是否相同
        void GetRemoteVersion(Action<string> result);

        void SetLocalVersion(string version);

        bool CheckAssetVersion(string localVersion, string remoteVersion);

        string GetLocalVersion();

        void Download(string url, Action<long> process, Action<bool, byte[]> resultCallBack);

    }
}