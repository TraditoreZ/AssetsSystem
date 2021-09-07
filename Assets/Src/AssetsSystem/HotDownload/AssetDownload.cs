using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    GameObject.DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private string m_remoteUrl;

        public IAssetHotDownload downloader { get; private set; }

        public string remoteUrl { get { return m_remoteUrl; } }

        public delegate void DownloadDelegate(EHotDownloadProgress progress);
        public event DownloadDelegate DownloadEvent;
        public delegate void DownloadProcessDelegate(long currtSize, long maxSize, long speed);
        public event DownloadProcessDelegate ProcessEvent;
        public delegate void ErrorDelegate(string error);
        public event ErrorDelegate ErrorEvent;
        public delegate void HotDownloadOverDelegate();
        public event HotDownloadOverDelegate HotDownloadOverEvent;

        private bool checkFileMD5;

        void Awake()
        {
            _instance = this;
        }


        public static void ResourceUpdateOnRemote(string remoteUrl, IAssetHotDownload assetHotDownload, bool checkMD5 = false)
        {
            instance.downloader = assetHotDownload;
            instance.m_remoteUrl = remoteUrl;
            instance.checkFileMD5 = checkMD5;
            if (!System.IO.Directory.Exists(AssetBundlePathResolver.instance.GetBundlePersistentFile()))
                System.IO.Directory.CreateDirectory(AssetBundlePathResolver.instance.GetBundlePersistentFile());
            instance.UpdateProcess(EHotDownloadProgress.CheckPersistentResource);
        }

        public static void CheckUpdate(Action<bool> versionCall)
        {
            instance.downloader.GetRemoteVersion((remoteVersion) =>
            {
                instance.downloader.GetLocalVersion((localVersion) =>
                {
                    instance.downloader.CheckRemoteVersion(localVersion, remoteVersion, (update) =>
                    {
                        versionCall(update);
                    });
                });
            });
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
                        DownloadAssets(arms[0].ToString(), arms[1] as ModifyData);
                        break;
                    case EHotDownloadProgress.DownloadManifest:
                        DownloadManifest(arms[0].ToString(), arms[1] as ModifyData);
                        break;
                    case EHotDownloadProgress.FinishDownload:
                        FinishDownload(arms[0].ToString());
                        break;
                    case EHotDownloadProgress.CheckUnzipBinary:
                        CheckUnzipBinary(arms[0].ToString());
                        break;
                    case EHotDownloadProgress.UnzipBinary:
                        UnzipBinary(arms[0].ToString());
                        break;
                    case EHotDownloadProgress.CheckFirstRun:
                        CheckFirstRun();
                        break;
                    case EHotDownloadProgress.FirstRunUnzipBinary:
                        FirstRunUnzipBinary();
                        break;
                    case EHotDownloadProgress.Over:
                        Debug.Log("ResourceUpdateFinish");
                        System.GC.Collect();
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
                    downloader.CheckRemoteVersion(localVersion, remoteVersion, (update) =>
                    {
                        if (update)
                        {
                            UpdateProcess(EHotDownloadProgress.DownloadModifyList, remoteVersion);
                        }
                        else
                        {
                            UpdateProcess(EHotDownloadProgress.Over);
                        }
                    });
                });
            });
        }

        void CheckPersistentResource()
        {
            if (downloader.CheckPersistentResource())
                UpdateProcess(EHotDownloadProgress.CheckFirstRun);
            else
                UpdateProcess(EHotDownloadProgress.ClearPersistentResource);
        }

        void ClearPersistentResource()
        {
            // 清理所有
            System.IO.Directory.Delete(AssetBundlePathResolver.instance.GetBundlePersistentFile(), true);
            System.IO.Directory.CreateDirectory(AssetBundlePathResolver.instance.GetBundlePersistentFile());
            UpdateProcess(EHotDownloadProgress.CheckFirstRun);
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
            if (HDResolver.IsPersistAssetNeedUpdate(ref data, checkFileMD5))
                UpdateProcess(EHotDownloadProgress.DownloadAssets, version, data);
            else
                UpdateProcess(EHotDownloadProgress.CheckUnzipBinary, version);
        }



        void DownloadAssets(string version, ModifyData data)
        {
            DownloadReqData[] datas = new DownloadReqData[data.datas.Length];
            for (int i = 0; i < data.datas.Length; i++)
            {
                ModifyData.ModifyCell cell = data.datas[i];
                string hashName = AssetBundlePathResolver.GetBundleUniqueHashName(cell.name, cell.bundleHash);
                datas[i] = new DownloadReqData()
                {
                    url = string.Format("{0}/{1}/{2}", remoteUrl, AssetBundlePathResolver.instance.GetBundlePlatformRuntime(), hashName),
                    filePath = AssetBundlePathResolver.instance.GetBundlePersistentFile(hashName),
                    size = cell.size
                };
                // 用于下载完成后解压用
                HDResolver.WriteFileLine(GetUnzipPath(), cell.name);
            }
            CoroutineHttpDownload.instance.DownloadFile(datas, (ok) =>
            {
                if (ok)
                {
                    UpdateProcess(EHotDownloadProgress.DownloadManifest, version, data);
                }
                else
                {
                    ErrorEvent?.Invoke("DownloadAssets Error:" + CoroutineHttpDownload.instance.ErrorMessage);
                }
            }, (downLoadCurrtSize, downLoadMaxSize, speedPreSecond) =>
            {
                ProcessEvent?.Invoke(downLoadCurrtSize, downLoadMaxSize, speedPreSecond);
            });
        }

        void DownloadManifest(string version, ModifyData data)
        {
            string url = string.Format("{0}/{1}_Data/v{2}_manifest", remoteUrl, AssetBundlePathResolver.instance.GetBundlePlatformRuntime(), version);
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
                    UpdateProcess(EHotDownloadProgress.CheckUnzipBinary, version);
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

        void CheckUnzipBinary(string version)
        {
            if (System.IO.File.Exists(GetUnzipPath()))
            {
                UpdateProcess(EHotDownloadProgress.UnzipBinary, version);
            }
            else
            {
                UpdateProcess(EHotDownloadProgress.FinishDownload, version);
            }
        }

        void UnzipBinary(string version)
        {
            StartCoroutine(IEUnzipBinary(() =>
            {
                UpdateProcess(EHotDownloadProgress.FinishDownload, version);
            }));
        }

        void FirstRunUnzipBinary()
        {
            AssetBundle ab = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleSourceFile(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
            AssetBundleManifest allManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            ulong offset = HDResolver.BundleOffset("bundle.rule");
            AssetBundle ruleAB = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleSourceFile(AssetBundlePathResolver.GetBundleUniqueHashName("bundle.rule", allManifest.GetAssetBundleHash("bundle.rule").ToString())), 0, offset);
            string[] commands = ruleAB.LoadAllAssets<TextAsset>().FirstOrDefault().text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            ab.Unload(true);
            ruleAB.Unload(true);
            var rules = new List<AssetBundleRule>();
            AssetBundleBuildConfig.ResolveRule(rules, commands);
            foreach (var rule in rules)
            {
                if (rule.options.Where(item => item.Contains("binary")).Count() > 0
                && rule.options.Where(item => item.Contains("unzip")).Count() > 0)
                {
                    Debug.Log("待解压本地资源:" + rule.packName);
                    HDResolver.WriteFileLine(GetUnzipPath(), rule.packName);
                }
            }
            if (File.Exists(GetUnzipPath()))
            {
                StartCoroutine(IEUnzipBinary(() =>
                {
                    UpdateProcess(EHotDownloadProgress.CheckRemoteVersion);
                }));
            }
            else
            {
                UpdateProcess(EHotDownloadProgress.CheckRemoteVersion);
            }

        }

        IEnumerator IEUnzipBinary(Action callBack)
        {
            yield return null;
            AssetBundle ab = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
            AssetBundleManifest allManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            ulong offset = HDResolver.BundleOffset("bundle.rule");
            AssetBundle ruleAB = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.GetBundleUniqueHashName("bundle.rule", allManifest.GetAssetBundleHash("bundle.rule").ToString())), 0, offset);
            string[] commands = ruleAB.LoadAllAssets<TextAsset>().FirstOrDefault().text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var rules = new List<AssetBundleRule>();
            AssetBundleBuildConfig.ResolveRule(rules, commands);

            string assetName = string.Empty;
            do
            {
                assetName = HDResolver.ReadFileLine(GetUnzipPath());
                if (!string.IsNullOrEmpty(assetName))
                {
                    foreach (var rule in rules)
                    {
                        if (assetName.Equals(rule.packName)
                        && rule.options.Where(item => item.Contains("binary")).Count() > 0
                        && rule.options.Where(item => item.Contains("unzip")).Count() > 0)
                        {
                            string assetHashPath = AssetBundlePathResolver.GetBundleUniqueHashName(assetName, allManifest.GetAssetBundleHash(assetName).ToString());
                            Debug.Log("解压资源:" + rule.packName + "\r\n哈希路径:" + assetHashPath);
                            UnzipCell(assetName, assetHashPath);
                            yield return null;
                        }
                    }
                }
                HDResolver.DeleteFileLine(GetUnzipPath());
            } while (!string.IsNullOrEmpty(assetName));
            ab.Unload(true);
            ruleAB.Unload(true);
            System.IO.File.Delete(GetUnzipPath());
            callBack?.Invoke();
        }

        void UnzipCell(string assetName, string fileName)
        {
            ulong offset = HDResolver.BundleOffset(assetName);
            AssetBundle ab = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(fileName), 0, offset);
            string binaryDataName = Path.GetFileNameWithoutExtension(assetName) + "_binaryData.txt";
            TextAsset taBinaryData = ab.LoadAsset<TextAsset>(binaryDataName);
            string[] assets = taBinaryData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var name in assets)
            {
                string targerPath = AssetBundlePathResolver.instance.GetBundlePersistentFile(name);
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(targerPath)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targerPath));
                }
                string bundlePath = "assets/" + name.ToLower() + ".bytes";
                TextAsset ta = ab.LoadAsset<TextAsset>(bundlePath);
                using (FileStream fs = new FileStream(targerPath, FileMode.CreateNew))
                {
                    fs.Write(ta.bytes, 0, ta.bytes.Length);
                    fs.Close();
                }
            }
            ab.Unload(true);
        }

        void CheckFirstRun()
        {
            if (downloader.IsFirstRun())
            {
                UpdateProcess(EHotDownloadProgress.FirstRunUnzipBinary);
            }
            else
            {
                UpdateProcess(EHotDownloadProgress.CheckRemoteVersion);
            }
        }


        string GetUnzipPath()
        {
            return AssetBundlePathResolver.instance.GetBundlePersistentFile("unzip.list");
        }

    }
}