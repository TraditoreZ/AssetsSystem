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
                    if (modifyCell.name.Equals(localAsset) && modifyCell.bundleHash.Equals(localManifest.GetAssetBundleHash(localAsset).ToString()))
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
        public static bool IsPersistAssetNeedUpdate(ref ModifyData modifyData, bool checkMD5)
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
                    if (!modifyCell.bundleHash.Equals(allManifest.GetAssetBundleHash(modifyCell.name).ToString()))
                    {
                        cells.Add(modifyCell);
                        Debug.Log("资源bundle md5变化, 需要热更新:" + modifyCell.name);
                    }
                    else if (!fileInfo.Length.Equals(modifyCell.size))
                    {
                        cells.Add(modifyCell);
                        Debug.Log("资源大小发生改变, 需要热更新:" + modifyCell.name);
                    }
                    else if (checkMD5 && !GetFileHash(assetPath).Equals(modifyCell.fileHash))
                    {
                        cells.Add(modifyCell);
                        Debug.Log("资源文件MD5发生改变, 需要热更新:" + modifyCell.name);
                    }
                }
                else
                {
                    cells.Add(modifyCell);
                    Debug.Log("资源不存在, 需要热更新:" + modifyCell.name);
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
            string targer = string.Empty;
            using (StreamReader sr = new StreamReader(path))
            {
                targer = sr.ReadToEnd();
                sr.Close();
            }
            return targer;
        }

        public static void WriteFileLine(string path, string line)
        {
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine(line);
                sw.Flush();
                sw.Close();
            }
        }

        public static string ReadFileLine(string path)
        {
            string line = string.Empty;
            using (StreamReader sr = new StreamReader(path))
            {
                line = sr.ReadLine();
                sr.Close();
            }
            return line;
        }

        public static void DeleteFileLine(string path)
        {
            string surplus = string.Empty;
            using (StreamReader sr = new StreamReader(path))
            {
                sr.ReadLine();
                surplus = sr.ReadToEnd();
                sr.Close();
            }
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.Write(surplus);
                sw.Flush();
                sw.Close();
            }
        }

        public static string GetFileHash(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    int len = (int)fs.Length;
                    byte[] data = new byte[len];
                    fs.Read(data, 0, len);
                    fs.Close();
                    MD5 md5 = new MD5CryptoServiceProvider();
                    byte[] result = md5.ComputeHash(data);
                    string fileMD5 = "";
                    foreach (byte b in result)
                    {
                        fileMD5 += Convert.ToString(b, 16);
                    }
                    return fileMD5;
                }
                // using (HashAlgorithm hash = HashAlgorithm.Create())
                // {
                //     using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //     {
                //         //哈希算法根据文本得到哈希码的字节数组 
                //         byte[] hashByte = hash.ComputeHash(file);
                //         hash.Dispose();
                //         //将字节数组装换为字符串 
                //         return BitConverter.ToString(hashByte);
                //     }
                // }
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError(e);
                return "";
            }
        }

        public static string[] GetBundleSourceFileBundles()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleSourceFile(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
            AssetBundleManifest allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] bundles = allManifest.GetAllAssetBundles();
            assetBundle.Unload(true);
            return bundles;
        }

    }
}