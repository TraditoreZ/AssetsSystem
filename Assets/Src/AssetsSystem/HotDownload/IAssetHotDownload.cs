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

        string GetLocalVersion();

        // void DownloadModifyList(string version, Action<ModifyData> modifyData);
        // // 更新列表   进度回调<当前文件位置   文件数量  当前文件字节  文件最大字节>  结果回调
        // void DownloadResources(ModifyData downloadData, Action<long, long> process, Action<bool> overCallBack);
        // 我准备用下面的这一个接口替换调上面的两个接口
        void Download(string url, Action<long> process, Action<bool, byte[]> resultCallBack);


    }
}