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

        bool CheckRemoteVersion(string localVersion, string remoteVersion);

        void GetLocalVersion(Action<string> result);
        // 接口返回是否保留Persistent下资源 可用于新包删除旧的缓存资源
        bool CheckPersistentResource();

        void Download(string url, Action<long> process, Action<bool, byte[]> resultCallBack);

    }
}