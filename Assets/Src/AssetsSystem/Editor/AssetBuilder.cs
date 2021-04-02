﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Linq;
using AssetSystem;

namespace AssetEditor
{
    public class AssetBuilder : Editor
    {
        //====================================================================================================================
        const string configPath = "Assets/Src/AssetsSystem/Editor/exampleCfg";
        const BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;//BuildAssetBundleOptions.ChunkBasedCompression;
        //==============================================================================================================
        private static AssetBundleRule[] rules;


        [MenuItem("AssetSystem/BuildAssetBundle[Windows]")]
        public static void BuildAssetBundleWindows()
        {
            BuildAssetBundle(BuildTarget.StandaloneWindows64);
        }


        [MenuItem("AssetSystem/BuildAssetBundle[Android]")]
        public static void BuildAssetBundleAndroid()
        {
            BuildAssetBundle(BuildTarget.Android);
        }


        [MenuItem("AssetSystem/BuildAssetBundle[IOS]")]
        public static void BuildAssetBundleIOS()
        {
            BuildAssetBundle(BuildTarget.iOS);
        }

        public static void BuildAssetBundle(BuildTarget buildTarget = BuildTarget.StandaloneWindows64, bool increment = false)
        {
            try
            {
                if (!increment)
                {
                    ClearBundles(GetOutPath(buildTarget));
                }
                if (!System.IO.Directory.Exists(GetOutPath(buildTarget)))
                {
                    System.IO.Directory.CreateDirectory(GetOutPath(buildTarget));
                }
                string[] unityAssetPaths = AssetDatabase.GetAllAssetPaths();
                List<string> assetPaths = new List<string>();
                foreach (var path in unityAssetPaths)
                {
                    if (Regex.IsMatch(path, @".+/.+\..+$"))
                    {
                        assetPaths.Add(path);
                    }
                }
                Dictionary<string, ABPackage> packsDic = BuildAssetBundlePack(assetPaths.ToArray());
                List<AssetBundleBuild> abbLists = new List<AssetBundleBuild>();
                int index = 0;
                int max = packsDic.Values.Count;
                foreach (var pack in packsDic.Values)
                {
                    EditorUtility.DisplayCancelableProgressBar("Create AssetPackage", string.Format("{0}     {1}:mb", pack.packageName, pack.size_MB), (float)index++ / max);
                    AssetBundleBuild abb = new AssetBundleBuild();
                    abb.assetBundleName = pack.packageName;
                    abb.assetNames = pack.assets.ToArray();
                    abbLists.Add(abb);
                }
                if (File.Exists(configPath.EndsWith(".txt") ? configPath : configPath + ".txt"))
                {

                    File.Copy(configPath.EndsWith(".txt") ? configPath : configPath + ".txt", Path.Combine(GetOutPath(buildTarget), "bundleRule.txt"), true);
                }
                else
                {
                    Debug.LogError("Not Find " + configPath);
                    throw new System.Exception();
                }
                // 如果运行时采用正则表达式 那么可以不需要这张表了
                //CreateABConfig(packsDic);
                //根据BuildSetting里面所激活的平台进行打包 设置过AssetBundleName的都会进行打包  
                //BuildPipeline.BuildAssetBundles(outputPath, options, buildTarget);
                // uint crc = 0;
                // BuildPipeline.GetCRCForAssetBundle(pack.packageName, out crc);
                // Debug.Log("Crc:" + crc);
                AssetBundleManifest oldManifest = null;
                if (File.Exists(Path.Combine(GetOutPath(buildTarget), AssetBundlePathResolver.instance.BundleSaveDirName)))
                {
                    AssetBundle oldManifestBundle = AssetBundle.LoadFromFile(Path.Combine(GetOutPath(buildTarget), AssetBundlePathResolver.instance.BundleSaveDirName));
                    oldManifest = oldManifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                }
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(GetOutPath(buildTarget), abbLists.ToArray(), options, buildTarget);
                if (oldManifest != null)
                {
                    foreach (var asset in manifest.GetAllAssetBundles())
                    {
                        if (!oldManifest.GetAssetBundleHash(asset).Equals(manifest.GetAssetBundleHash(asset)))
                        {
                            CreateHashCheckTable(asset, oldManifest.GetAssetBundleHash(asset).ToString(), manifest.GetAssetBundleHash(asset).ToString(), buildTarget);
                        }
                    }
                }
                Move2Project(buildTarget);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }


        static Dictionary<string, ABPackage> BuildAssetBundlePack(string[] assets)
        {
            rules = AssetBundleBuildConfig.GetRules(configPath);
            Dictionary<string, ABPackage> packs = new Dictionary<string, ABPackage>();
            foreach (var path in assets)
            {
                CreateABPackByRule(path, packs);
            }
            return packs;
        }

        // static void CreateABConfig(Dictionary<string, ABPackage> packsDic)
        // {
        //     AssetSystem.AssetConfig assetCfg = new AssetSystem.AssetConfig();
        //     foreach (var pack in packsDic.Values)
        //     {
        //         string options = string.Join("", pack.options);
        //         assetCfg.packInfos.Add(pack.packageName, options);
        //         foreach (var path in pack.assets)
        //         {
        //             assetCfg.assetMaps.Add(path, pack.packageName);
        //         }
        //     }
        //     FileStream fs = new FileStream(GetOutPath() + "/AssetBundleConf.txt", FileMode.Create);
        //     StreamWriter sw = new StreamWriter(fs);
        //     sw.Write(JsonUtility.ToJson(assetCfg));
        //     sw.Flush();
        //     sw.Close();
        //     fs.Close();
        // }

        // hash表单 记录bundle的所有修改 可用于任意时刻切曾量包
        static void CreateHashCheckTable(string package, string oldHash, string newHash, BuildTarget buildTarget)
        {
            using (StreamWriter sw = new StreamWriter(GetOutPath(buildTarget) + "/HashCheck.cfg", true))
            {
                string command = string.Format("{0}:{1}>{2}", package, oldHash, newHash);
                sw.WriteLine(command);
                Debug.Log(command);
                sw.Flush();
                sw.Close();
            }
        }

        static void CreateABPackByRule(string assetPath, Dictionary<string, ABPackage> packs)
        {

            if (assetPath.EndsWith(".cs"))
                return;
            foreach (var rule in rules)
            {
                var info = AssetBundleBuildConfig.MatchAssets(assetPath, rule);
                if (info != null)
                {
                    ABPackage pack = null;
                    if (!packs.TryGetValue(info.packName, out pack))
                    {
                        pack = new ABPackage();
                        pack.packageName = info.packName;
                        pack.options = info.options;
                        packs.Add(info.packName, pack);
                    }
                    if (pack.assets.Add(assetPath))
                    {
                        pack.size_MB += (new System.IO.FileInfo(assetPath).Length / 1048576f); // byte => mb
                    }
                }
            }
        }

        static string GetOutPath(BuildTarget buildTarget)
        {
            return AssetBundlePathResolver.BundleOutputPath(buildTarget);
        }

        static void Move2Project(BuildTarget buildTarget)
        {
            string targer = string.Format("{0}/StreamingAssets/{1}", Application.dataPath, AssetBundlePathResolver.instance.BundleSaveDirName);
            ClearBundles(targer);
            CopyBundle(GetOutPath(buildTarget), targer, true);
            //bundle移动到streaming内 跟随主体打包
            //文件的复制粘贴操作即可
        }

        static void Move2Package()
        {
            //bundle做增量包
        }

        static void CopyBundle(string srcdir, string dstdir, bool overwrite)
        {
            string todir = Path.Combine(dstdir, Path.GetFileName(srcdir));
            if (!Directory.Exists(todir))
                Directory.CreateDirectory(todir);
            foreach (var s in Directory.GetFiles(srcdir))
            {
                if (!s.EndsWith(".meta") && !s.EndsWith(".manifest"))
                {
                    File.Copy(s, Path.Combine(todir, Path.GetFileName(s)), overwrite);
                }
            }
            foreach (var s in Directory.GetDirectories(srcdir))
                CopyBundle(s, todir, overwrite);
        }


        static void ClearBundles(string dir)
        {
            if (Directory.Exists(dir))
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                di.Delete(true);
            }
        }
    }
}