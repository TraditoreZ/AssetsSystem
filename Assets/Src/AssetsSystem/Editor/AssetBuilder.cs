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

        const string cfg_AllBundleMD5 = "bundleMD5.cfg";
        // 对应runtime也需要修改
        //==============================================================================================================
        private static AssetBundleRule[] rules;

        public static void BuildManifest(string version, BuildTarget buildTarget)
        {
            if (!System.IO.Directory.Exists(GetOutDataPath(buildTarget)))
            {
                System.IO.Directory.CreateDirectory(GetOutDataPath(buildTarget));
            }
            File.Copy(Path.Combine(GetOutPath(buildTarget), AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget)), Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(version)), true);
        }

        public static void BuildAssetBundle(BuildTarget buildTarget, bool increment)
        {
            AssetEditorData data = null;
            var paths = AssetDatabase.FindAssets("t:AssetEditorData");
            if (paths != null && paths.Length > 0)
            {
                data = AssetDatabase.LoadAssetAtPath<AssetEditorData>(AssetDatabase.GUIDToAssetPath(paths[0]));
            }
            else
            {
                throw new System.Exception("Not find EditorData.asset");
            }
            BuildAssetBundle(data.splitConfigPath, data.options, buildTarget, increment);
        }

        public static void GenerateModifyList(BuildTarget buildTarget, string currtVersion, string sourceVersion)
        {
            string sourcePath = Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(sourceVersion));
            string currtPath = Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(currtVersion));
            if (!System.IO.File.Exists(currtPath))
            {
                throw new System.Exception("Not find: " + currtPath);
            }
            if (!System.IO.File.Exists(sourcePath))
            {
                throw new System.Exception("Not find: " + sourcePath);
            }
            Dictionary<string, string> modifyList = null;
            if (HDResolver.CheckModify(currtPath, sourcePath, out modifyList))
            {
                CreateModifyList(modifyList, buildTarget, Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetModifyListName(currtVersion)));
            }
        }


        public static void BuildAssetBundle(string configPath, BuildAssetBundleOptions options, BuildTarget buildTarget = BuildTarget.StandaloneWindows64, bool increment = false)
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
                Dictionary<string, ABPackage> packsDic = BuildAssetBundlePack(configPath, assetPaths.ToArray());
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

                watch.Reset();
                watch.Start();
                BuildPipeline.BuildAssetBundles(GetOutPath(buildTarget), abbLists.ToArray(), options, buildTarget);
                watch.Stop();
                Debug.Log("BuildAssetBundles Time:" + RevertToTime(watch.ElapsedMilliseconds));

            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                watch.Reset();
            }
        }


        static Dictionary<string, ABPackage> BuildAssetBundlePack(string configPath, string[] assets)
        {
            rules = AssetBundleBuildConfig.GetRules(configPath);
            Dictionary<string, ABPackage> packs = new Dictionary<string, ABPackage>();
            foreach (var path in assets)
            {
                CreateABPackByRule(path, packs);
            }
            return packs;
        }


        // hash表单 记录bundle的所有修改 可用于任意时刻切曾量包
        static void CreateModifyList(Dictionary<string, string> bundleChangedDic, BuildTarget buildTarget, string outPath)
        {
            ModifyData modifyJson = new ModifyData();
            modifyJson.datas = new ModifyData.ModifyCell[bundleChangedDic.Count];

            int index = 0;
            foreach (var item in bundleChangedDic)
            {
                ModifyData.ModifyCell cell = new ModifyData.ModifyCell();
                modifyJson.datas[index++] = cell;
                cell.name = item.Key;
                cell.bundleHash = item.Value;
                cell.fileHash = HDResolver.GetFileHash(System.IO.Path.Combine(GetOutPath(buildTarget), cell.name));
                cell.size = (new System.IO.FileInfo(System.IO.Path.Combine(GetOutPath(buildTarget), cell.name))).Length;
            }
            using (StreamWriter sw = new StreamWriter(outPath, false))
            {
                sw.Write(JsonUtility.ToJson(modifyJson));
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
                if (info.HasValue)
                {
                    ABPackage pack = null;
                    if (!packs.TryGetValue(info.Value.packName, out pack))
                    {
                        pack = new ABPackage();
                        pack.packageName = info.Value.packName;
                        pack.options = info.Value.options;
                        packs.Add(info.Value.packName, pack);
                    }
                    if (pack.assets.Add(assetPath) && File.Exists(assetPath))
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

        static string GetOutDataPath(BuildTarget buildTarget)
        {
            return AssetBundlePathResolver.BundleOutputPath(buildTarget) + "_Data";
        }

        public static void Move2Project(BuildTarget buildTarget)
        {
            string targer = string.Format("{0}/StreamingAssets/{1}", Application.dataPath, AssetBundlePathResolver.instance.BundleSaveDirName);
            ClearBundles(targer);
            CopyBundle(GetOutPath(buildTarget), targer, true);
            AssetDatabase.Refresh();
        }

        public static void Move2Package(string version, BuildTarget buildTarget, string packagePath)
        {
            if (!System.IO.Directory.Exists(Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget) + "_Data")))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget) + "_Data"));
            }
            // 复制Bundle
            CopyBundle(GetOutPath(buildTarget), packagePath, true);
            // 复制Mainfest信息
            if (System.IO.File.Exists(Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(version))))
            {
                File.Copy(Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(version)), Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget) + "_Data", HDResolver.GetManifestName(version)), true);
            }
            // 复制更新表
            if (System.IO.File.Exists(Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetModifyListName(version))))
            {
                File.Copy(Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetModifyListName(version)), Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget) + "_Data", HDResolver.GetModifyListName(version)), true);
            }
            AssetDatabase.Refresh();
        }

        public static void CreateAssetVersion(BuildTarget buildTarget, string version)
        {
            string outPath = Path.Combine(GetOutPath(buildTarget), "version.txt");
            HDResolver.WriteFile(outPath, version);
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
        static string RevertToTime(long l)
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