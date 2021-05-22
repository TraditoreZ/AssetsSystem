using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetSystem
{

    public class AssetDownload : MonoBehaviour
    {
        private static AssetDownload _instance;

        public static AssetDownload instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AssetDownload");
                    _instance = go.AddComponent<AssetDownload>();
                }
                return _instance;
            }
        }

        private string m_remoteUrl;

        public IAssetHotDownload downloader { get; private set; }

        public string remoteUrl { get { return m_remoteUrl; } }

        public delegate void DownloadDelegate(EHotDownloadProgress progress);
        public event DownloadDelegate DownloadEvent;
        public delegate void DownloadProcessDelegate(string assetName, long currtSize, long maxSize, int index, int count);
        public event DownloadProcessDelegate ProcessEvent;
        public delegate void ErrorDelegate(string error);
        public event ErrorDelegate ErrorEvent;
        public delegate void HotDownloadOverDelegate();
        public event HotDownloadOverDelegate HotDownloadOverEvent;

        private long downLoadCurrtSize;
        private long downLoadMaxSize;


        void Awake()
        {
            _instance = this;
        }


        public static void ResourceUpdateOnRemote(string remoteUrl, IAssetHotDownload assetHotDownload)
        {
            instance.downloader = assetHotDownload;
            instance.m_remoteUrl = remoteUrl;
            if (!System.IO.Directory.Exists(AssetBundlePathResolver.instance.GetBundlePersistentFile()))
                System.IO.Directory.CreateDirectory(AssetBundlePathResolver.instance.GetBundlePersistentFile());
            instance.UpdateProcess(EHotDownloadProgress.CheckPersistentResource);
        }

        void UpdateProcess(EHotDownloadProgress progress, params object[] arms)
        {
            DownloadEvent?.Invoke(progress);
            try
            {
                switch (progress)
                {
                    case EHotDownloadProgress.CheckPersistentResource:
                        CheckPersistentResource();
                        break;
                    case EHotDownloadProgress.ClearPersistentResource:
                        ClearPersistentResource();
                        break;
                    case EHotDownloadProgress.CheckRemoteVersion:
                        CheckRemoteVersion();
                        break;
                    case EHotDownloadProgress.CheckBreakpoint:
                        CheckBreakpoint(arms[0].ToString(), arms[1] as ModifyData);
                        break;
                    case EHotDownloadProgress.DownloadModifyList:
                        DownloadModifyList(arms[0].ToString());
                        break;
                    case EHotDownloadProgress.CullingLocalResource:
                        CullingLocalResource(arms[0].ToString(), arms[1] as ModifyData);
                        break;
                    case EHotDownloadProgress.CompareAssetHash:
                        CompareAssetHash(arms[0].ToString(), arms[1] as ModifyData);
                        break;
                    case EHotDownloadProgress.DownloadAssets:
                        DownloadAssets(arms[0].ToString(), arms[1] as ModifyData, (int)arms[2]);
                        break;
                    case EHotDownloadProgress.DownloadManifest:
                        DownloadManifest(arms[0].ToString());
                        break;
                    case EHotDownloadProgress.FinishDownload:
                        FinishDownload(arms[0].ToString());
                        break;
                    case EHotDownloadProgress.Over:
                        Debug.Log("ResourceUpdateFinish");
                        HotDownloadOverEvent?.Invoke();
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                ErrorEvent?.Invoke(e.ToString());
            }
        }

        void CheckRemoteVersion()
        {
            downloader.GetRemoteVersion((remoteVersion) =>
            {
                downloader.GetLocalVersion((localVersion) =>
                {
                    if (downloader.CheckRemoteVersion(localVersion, remoteVersion))
                        UpdateProcess(EHotDownloadProgress.Over);
                    else
                        UpdateProcess(EHotDownloadProgress.DownloadModifyList, remoteVersion);
                });
            });
        }

        void CheckPersistentResource()
        {
            if (downloader.CheckPersistentResource())
                UpdateProcess(EHotDownloadProgress.CheckRemoteVersion);
            else
                UpdateProcess(EHotDownloadProgress.ClearPersistentResource);
        }

        void ClearPersistentResource()
        {
            // 清理所有
            System.IO.Directory.Delete(AssetBundlePathResolver.instance.GetBundlePersistentFile(), true);
            System.IO.Directory.CreateDirectory(AssetBundlePathResolver.instance.GetBundlePersistentFile());
            UpdateProcess(EHotDownloadProgress.CheckRemoteVersion);
        }

        void DownloadModifyList(string version)
        {
            string url = string.Format("{0}/{1}_Data/v{2}_modify.json", remoteUrl, AssetBundlePathResolver.instance.GetBundlePlatformRuntime(), version);
            downloader.Download(url, null, (ok, bytes) =>
            {
                if (ok)
                {
                    ModifyData data = JsonUtility.FromJson<ModifyData>(System.Text.Encoding.UTF8.GetString(bytes));
                    UpdateProcess(EHotDownloadProgress.CullingLocalResource, version, data);
                }
                else
                {
                    ErrorEvent?.Invoke("DownloadModifyList Fail");
                }
            });
        }


        void CullingLocalResource(string version, ModifyData data)
        {
            if (HDResolver.CullingLocalResource(ref data))
                UpdateProcess(EHotDownloadProgress.CompareAssetHash, version, data);
            else
                ErrorEvent?.Invoke("CullingLocalResource Fail");
        }


        void CompareAssetHash(string version, ModifyData data)
        {
            if (HDResolver.IsPersistAssetNeedUpdate(ref data))
                UpdateProcess(EHotDownloadProgress.CheckBreakpoint, version, data);
            else
                UpdateProcess(EHotDownloadProgress.FinishDownload, version);
        }

        void CheckBreakpoint(string version, ModifyData data)
        {
            int index = 0;
            if (System.IO.File.Exists(GetBreakpointPath(version)))
            {
                int.TryParse(HDResolver.ReadFile(GetBreakpointPath(version)), out index);
            }
            if (index < 0 || index > data.datas.Length)
            {
                ErrorEvent?.Invoke("DownloadAssets Fail index out of bounds");
                return;
            }
            downLoadMaxSize = 0;
            for (int i = index; i < data.datas.Length; i++)
                downLoadMaxSize += data.datas[i].size;
            UpdateProcess(EHotDownloadProgress.DownloadAssets, version, data, index);
        }


        void DownloadAssets(string version, ModifyData data, int index)
        {
            string assetName = data.datas[index].name;
            string url = string.Format("{0}/{1}/{2}", remoteUrl, AssetBundlePathResolver.instance.GetBundlePlatformRuntime(), assetName);
            int nextIndex = index + 1;
            downloader.Download(url, (currtSize) =>
            {
                ProcessEvent?.Invoke(assetName, downLoadCurrtSize + currtSize, downLoadMaxSize, nextIndex, data.datas.Length);
            },
            (ok, bytes) =>
            {
                if (ok)
                {
                    string localPath = AssetBundlePathResolver.instance.GetBundlePersistentFile(assetName);
                    using (System.IO.FileStream fs = new System.IO.FileStream(localPath, System.IO.FileMode.Create))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();
                    }
                    downLoadCurrtSize += bytes.Length;
                    if (nextIndex >= data.datas.Length)
                    {
                        System.IO.File.Delete(GetBreakpointPath(version));
                        UpdateProcess(EHotDownloadProgress.DownloadManifest, version);
                    }
                    else
                    {
                        HDResolver.WriteFile(GetBreakpointPath(version), (nextIndex).ToString());
                        DownloadAssets(version, data, nextIndex);
                    }
                }
                else
                {
                    ErrorEvent?.Invoke("DownloadAssets Fail");
                }
            });
        }

        void DownloadManifest(string version)
        {
            string url = string.Format("{0}/{1}/{2}", remoteUrl, AssetBundlePathResolver.instance.GetBundlePlatformRuntime(), AssetBundlePathResolver.instance.GetBundlePlatformRuntime());
            downloader.Download(url, null, (ok, bytes) =>
            {
                if (ok)
                {
                    string localPath = AssetBundlePathResolver.instance.GetBundlePersistentFile(AssetBundlePathResolver.instance.GetBundlePlatformRuntime());
                    using (System.IO.FileStream fs = new System.IO.FileStream(localPath, System.IO.FileMode.Create))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Close();
                    }
                    UpdateProcess(EHotDownloadProgress.FinishDownload, version);
                }
                else
                {
                    ErrorEvent?.Invoke("DownloadManifest Fail");
                }
            });
        }

        void FinishDownload(string version)
        {
            downloader.SetLocalVersion(version);
            UpdateProcess(EHotDownloadProgress.CheckRemoteVersion);
        }


        string GetBreakpointPath(string version)
        {
            return AssetBundlePathResolver.instance.GetBundlePersistentFile(string.Format("v{0}.breakpoint", version));
        }

    }
}