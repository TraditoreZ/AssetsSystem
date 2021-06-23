﻿using System;
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
        public delegate void DownloadProcessDelegate(string assetName, long currtSize, long maxSize, int index, int count);
        public event DownloadProcessDelegate ProcessEvent;
        public delegate void ErrorDelegate(string error);
        public event ErrorDelegate ErrorEvent;
        public delegate void HotDownloadOverDelegate();
        public event HotDownloadOverDelegate HotDownloadOverEvent;

        private long downLoadCurrtSize;
        private long downLoadMaxSize;

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
                UpdateProcess(EHotDownloadProgress.CheckBreakpoint, version, data);
            else
                UpdateProcess(EHotDownloadProgress.CheckUnzipBinary, version);
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
                index = 0;
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
            downLoadCurrtSize = 0;
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
                    HDResolver.WriteFileLine(GetUnzipPath(), assetName);
                    downLoadCurrtSize += bytes.Length;
                    if (nextIndex >= data.datas.Length)
                    {
                        System.IO.File.Delete(GetBreakpointPath(version));
                        UpdateProcess(EHotDownloadProgress.DownloadManifest, version, data);
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

        void DownloadManifest(string version, ModifyData data)
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
            AssetBundle ruleAB = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleSourceFile("bundle.rule"));
            string[] commands = ruleAB.LoadAllAssets<TextAsset>().FirstOrDefault().text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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
            AssetBundle ruleAB = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime("bundle.rule"));
            string[] commands = ruleAB.LoadAllAssets<TextAsset>().FirstOrDefault().text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            ruleAB.Unload(true);
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
                            Debug.Log("解压资源:" + rule.packName);
                            UnzipCell(rule.packName);
                            yield return null;
                        }
                    }
                }
                HDResolver.DeleteFileLine(GetUnzipPath());
            } while (!string.IsNullOrEmpty(assetName));
            System.IO.File.Delete(GetUnzipPath());
            callBack?.Invoke();
        }

        void UnzipCell(string assetName)
        {
            AssetBundle ab = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(assetName));
            string[] names = ab.GetAllAssetNames();
            foreach (var name in names)
            {
                string targerPath = AssetBundlePathResolver.instance.GetBundlePersistentFile(name.Substring(0, name.Length - 6).Replace("assets/", ""));
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(targerPath)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targerPath));
                }
                TextAsset ta = ab.LoadAsset<TextAsset>(name);
                using (FileStream fs = new FileStream(targerPath, FileMode.OpenOrCreate))
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

        string GetBreakpointPath(string version)
        {
            return AssetBundlePathResolver.instance.GetBundlePersistentFile(string.Format("v{0}.breakpoint", version));
        }

        string GetUnzipPath()
        {
            return AssetBundlePathResolver.instance.GetBundlePersistentFile("unzip.list");
        }

    }
}