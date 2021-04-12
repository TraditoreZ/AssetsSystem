using System.Collections;
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
        const string cfg_AllBundleMD5 = "bundleMD5.cfg";
        // 对应runtime也需要修改
        //==============================================================================================================
        private static AssetBundleRule[] rules;


        [MenuItem("AssetSystem/BuildAssetBundle[Windows]")]
        public static void BuildAssetBundleWindows()
        {
            BuildAssetBundle(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("AssetSystem/BuildAssetBundle Increment [Windows]")]
        public static void BuildAssetBundleWindowsIncrement()
        {
            BuildAssetBundle(BuildTarget.StandaloneWindows64, true);
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
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
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
                watch.Reset();
                watch.Start();
                string[] unityAssetPaths = AssetDatabase.GetAllAssetPaths();
                watch.Stop();
                Debug.Log("GetAllAssetPaths Time:" + RevertToTime(watch.ElapsedMilliseconds));
                List<string> assetPaths = new List<string>();
                foreach (var path in unityAssetPaths)
                {
                    if (Regex.IsMatch(path, @".+/.+\..+$"))
                    {
                        assetPaths.Add(path);
                    }
                }
                watch.Reset();
                watch.Start();
                Dictionary<string, ABPackage> packsDic = BuildAssetBundlePack(assetPaths.ToArray());
                watch.Stop();
                Debug.Log("BuildAssetBundlePack Time:" + RevertToTime(watch.ElapsedMilliseconds));
                List<AssetBundleBuild> abbLists = new List<AssetBundleBuild>();
                int index = 0;
                int max = packsDic.Values.Count;
                double bundlesize = 0;
                foreach (var pack in packsDic.Values)
                {
                    EditorUtility.DisplayCancelableProgressBar(string.Format("AssetBundleBuild[{0}/{1}]               {2:f2} mb", index, max, bundlesize += pack.size_MB), pack.packageName, (float)index++ / max);
                    AssetBundleBuild abb = new AssetBundleBuild();
                    abb.assetBundleName = pack.packageName;
                    abb.assetNames = pack.assets.ToArray();
                    abbLists.Add(abb);
                }
                AssetBundleBuild configABB = new AssetBundleBuild();
                configABB.assetBundleName = "bundle.rule";
                configABB.assetNames = new string[] { configPath.EndsWith(".txt") ? configPath : configPath + ".txt" };
                abbLists.Add(configABB);

                AssetBundleManifest oldManifest = null;
                if (File.Exists(Path.Combine(GetOutPath(buildTarget), AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget))))
                {
                    AssetBundle oldManifestBundle = AssetBundle.LoadFromFile(Path.Combine(GetOutPath(buildTarget), AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget)));
                    oldManifest = oldManifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                }
                watch.Reset();
                watch.Start();
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(GetOutPath(buildTarget), abbLists.ToArray(), options, buildTarget);
                watch.Stop();
                Debug.Log("BuildAssetBundles Time:" + RevertToTime(watch.ElapsedMilliseconds));
                foreach (var asset in manifest.GetAllAssetBundles())
                {
                    CreateBundleFrom(asset, manifest.GetAssetBundleHash(asset).ToString(), buildTarget);
                    if (oldManifest != null && !oldManifest.GetAssetBundleHash(asset).Equals(manifest.GetAssetBundleHash(asset)))
                    {
                        CreateHashCheck(asset, oldManifest.GetAssetBundleHash(asset).ToString(), manifest.GetAssetBundleHash(asset).ToString(), buildTarget);
                    }
                }
                Move2Project(buildTarget);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                watch.Reset();
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

        static void CreateBundleFrom(string package, string hash, BuildTarget buildTarget)
        {
            string command = string.Format("{0}:{1}", package, hash);
            using (StreamWriter sw = new StreamWriter(Path.Combine(GetOutPath(buildTarget), cfg_AllBundleMD5), true))
            {
                sw.WriteLine(command);
                sw.Flush();
                sw.Close();
            }
        }

        // hash表单 记录bundle的所有修改 可用于任意时刻切曾量包
        static void CreateHashCheck(string package, string oldHash, string newHash, BuildTarget buildTarget)
        {
            string command = string.Format("{0}:{1}-{2}", package, oldHash, newHash);
            Debug.Log("Package Changed:" + command);
            using (StreamWriter sw = new StreamWriter(Path.Combine(GetOutPath(buildTarget), AssetBundlePathResolver.instance.BundleBillName), true))
            {
                sw.WriteLine(command);
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
                if (!s.EndsWith(".meta") && !s.EndsWith(".manifest") && !s.EndsWith(cfg_AllBundleMD5))
                {
                    File.Copy(s, Path.Combine(todir, Path.GetFileName(s)), overwrite);
                }
            }
            foreach (var s in Directory.GetDirectories(srcdir))
                CopyBundle(s, todir, overwrite);
        }


        static void ClearBundles(string rootDir)
        {
            if (Directory.Exists(rootDir))
            {
                DirectoryInfo di = new DirectoryInfo(rootDir);
                di.Delete(true);
            }
        }

        //转换为时分秒格式
        private static string RevertToTime(long l)
        {
            int hour = 0;
            int minute = 0;
            int second = 0;
            second = (int)(l / 1000);
            if (second > 60)
            {
                minute = second / 60;
                second = second % 60;
            }
            if (minute > 60)
            {
                hour = minute / 60;
                minute = minute % 60;
            }
            return (hour.ToString() + ":" + minute.ToString() + ":" + second.ToString());
        }

    }
}