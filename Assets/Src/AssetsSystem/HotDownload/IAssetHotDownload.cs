using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public interface IAssetHotDownload
{
    // 回调参数 第一个为最新版本信息， 第二个为版本是否相同
    void GetRemoteVersion(Action<string> result);

    string GetLocalVersion();

    void DownloadModifyList(string version, Action<string> modifyData);


}
