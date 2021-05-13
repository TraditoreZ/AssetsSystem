using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace AssetSystem
{
    public class HDResolver
    {
        public static bool CheckModify(string compareManifestPath, string beCompareManifestPath, out Dictionary<string, string> modifyList)
        {
            modifyList = null;
            AssetBundle beCompareManifestBundle = AssetBundle.LoadFromFile(beCompareManifestPath);
            if (beCompareManifestBundle == null)
                return false;
            AssetBundleManifest beCompareManifest = beCompareManifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (beCompareManifest == null)
                return false;
            beCompareManifestBundle.Unload(false);
            AssetBundle compareManifestBundle = AssetBundle.LoadFromFile(compareManifestPath);
            if (compareManifestBundle == null)
                return false;
            AssetBundleManifest compareManifest = compareManifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (compareManifest == null)
                return false;
            compareManifestBundle.Unload(false);
            modifyList = new Dictionary<string, string>();
            string[] compareBundles = compareManifest.GetAllAssetBundles();
            foreach (var compareBundle in compareBundles)
            {
                if (!beCompareManifest.GetAssetBundleHash(compareBundle).Equals(compareManifest.GetAssetBundleHash(compareBundle)))
                {
                    string bundleHash = compareManifest.GetAssetBundleHash(compareBundle).ToString();
                    if (modifyList.ContainsKey(compareBundle))
                    {
                        modifyList[compareBundle] = bundleHash;
                    }
                    else
                    {
                        modifyList.Add(compareBundle, bundleHash);
                    }
                }
            }
            return true;
        }

        public static bool CullingLocalResource(ref ModifyData modifyData)
        {
            AssetBundle localBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleSourceFile(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
            if (localBundle == null)
                return false;
            AssetBundleManifest localManifest = localBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (localManifest == null)
                return false;
            if (modifyData == null || modifyData.datas == null)
                return false;
            string[] localAssetbundles = localManifest.GetAllAssetBundles();
            List<ModifyData.ModifyCell> cells = new List<ModifyData.ModifyCell>();
            bool downloadFlag = false;
            foreach (var modifyCell in modifyData.datas)
            {
                downloadFlag = true;
                foreach (var localAsset in localAssetbundles)
                {
                    if (modifyCell.name.Equals(localAsset) && modifyCell.hash.Equals(localManifest.GetAssetBundleHash(localAsset)))
                    {
                        downloadFlag = false;
                    }
                }
                if (downloadFlag)
                {
                    cells.Add(modifyCell);
                }
            }
            modifyData.datas = cells.ToArray();
            localBundle.Unload(true);
            return true;
        }

        // 返回 是否需要更新资源
        public static bool IsPersistAssetNeedUpdate(ref ModifyData modifyData)
        {
            if (modifyData == null || modifyData.datas == null || modifyData.datas.Length == 0)
                return false;
            AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
            AssetBundleManifest allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            List<ModifyData.ModifyCell> cells = new List<ModifyData.ModifyCell>();
            foreach (var modifyCell in modifyData.datas)
            {
                string assetPath = AssetBundlePathResolver.instance.GetBundlePersistentFile(modifyCell.name);
                FileInfo fileInfo = new FileInfo(assetPath);
                if (fileInfo.Exists)
                {
                    if (fileInfo.Length != modifyCell.size)
                    {
                        cells.Add(modifyCell);
                    }
                    else if (!allManifest.GetAssetBundleHash(modifyCell.name).ToString().Equals(modifyCell.hash))
                    {  // md5不一致
                        cells.Add(modifyCell);
                    }
                }
                else
                {
                    cells.Add(modifyCell);
                }
            }
            allManifest = null;
            assetBundle.Unload(true);
            modifyData.datas = cells.ToArray();
            return cells.Count > 0;
        }

        public static string GetManifestName(string version)
        {
            return "v" + version + "_manifest";
        }

        public static string GetModifyListName(string version)
        {
            return "v" + version + "_modify.json";
        }


        public static void WriteFile(string path, string data)
        {
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.Write(data);
                sw.Flush();
                sw.Close();
            }
        }

        public static string ReadFile(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }

    }
}