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

        public static void BuildAssetBundle(string verson, BuildTarget buildTarget)
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
            if (!string.IsNullOrEmpty(verson))
            {
                data.version = verson;
            }
            BuildAssetBundle(data.splitConfigPath, data.options, buildTarget);
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


        public static void BuildAssetBundle(string configPath, BuildAssetBundleOptions options, BuildTarget buildTarget = BuildTarget.StandaloneWindows64)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            Dictionary<string, ABPackage> packsDic = null;
            try
            {
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
                packsDic = BuildAssetBundlePack(configPath, assetPaths.ToArray());
                watch.Stop();
                Debug.Log("BuildAssetBundlePack Time:" + RevertToTime(watch.ElapsedMilliseconds));
                watch.Reset();
                watch.Start();
                GenerateBinaryAsset(packsDic.Values.ToArray());
                watch.Stop();
                Debug.Log("GenerateBinaryAsset Time:" + RevertToTime(watch.ElapsedMilliseconds));
                List<AssetBundleBuild> abbLists = new List<AssetBundleBuild>();
                int index = 0;
                int max = packsDic.Values.Count;
                double bundlesize = 0;
                foreach (var pack in packsDic.Values)
                {
                    EditorUtility.DisplayCancelableProgressBar(string.Format("AssetBundleBuild[{0}/{1}]               {2:f2} mb", index, max, bundlesize += pack.size_MB), pack.packageName, (float)index++ / max);
                    AssetBundleBuild abb = new AssetBundleBuild();
                    abb.assetBundleName = pack.packageName;

                    if (pack.binary)
                    {
                        List<string> assets = pack.assets.ToList();
                        for (int i = 0; i < assets.Count; i++)
                        {
                            string path = assets[i];
                            assets[i] = path + ".bytes";
                        }
                        assets.Add(GetBinaryDataPath(abb.assetBundleName).Replace(Application.dataPath, "Assets"));
                        abb.assetNames = assets.ToArray();
                    }
                    else
                    {
                        abb.assetNames = pack.assets.ToArray();
                    }
                    abbLists.Add(abb);
                }
                AssetBundleBuild configABB = new AssetBundleBuild();
                string rulePath = configPath.Replace(Application.dataPath, "Assets");
                configABB.assetBundleName = "bundle.rule";
                configABB.assetNames = new string[] { rulePath.EndsWith(".txt") ? rulePath : rulePath + ".txt" };
                abbLists.Add(configABB);

                watch.Reset();
                watch.Start();
                BuildPipeline.BuildAssetBundles(GetOutPath(buildTarget), abbLists.ToArray(), options, buildTarget);
                watch.Stop();
                Debug.Log("BuildAssetBundles Time:" + RevertToTime(watch.ElapsedMilliseconds));

            }
            finally
            {
                if (packsDic != null)
                {
                    DeleteBinaryAsset(packsDic.Values.ToArray());
                }
                watch.Reset();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                System.GC.Collect();
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
                cell.size = (new System.IO.FileInfo(System.IO.Path.Combine(GetOutPath(buildTarget), cell.name))).Length;
            }
            // 将bundle.rule放入最后一位,这样更新的时候rule也是最后一个更新
            for (int i = 0; i < modifyJson.datas.Length; i++)
            {
                if (modifyJson.datas[i].name.Equals("bundle.rule"))
                {
                    ModifyData.ModifyCell temp = modifyJson.datas[i];
                    modifyJson.datas[i] = modifyJson.datas[modifyJson.datas.Length - 1];
                    modifyJson.datas[modifyJson.datas.Length - 1] = temp;
                    break;
                }
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
                        pack.binary = info.Value.options.Where(item => item.Contains("binary")).Count() > 0;
                        packs.Add(info.Value.packName, pack);
                    }
                    if (pack.assets.Add(assetPath) && File.Exists(assetPath))
                    {
                        pack.size_MB += ((new System.IO.FileInfo(assetPath).Length + HDResolver.BundleOffset(Path.GetFileName(assetPath)) / 1048576f)); // byte => mb
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

        public static void Move2Project(string version, BuildTarget buildTarget)
        {
            string targer = string.Format("{0}/StreamingAssets/{1}", Application.dataPath, AssetBundlePathResolver.instance.BundleSaveDirName);
            ClearBundles(targer);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(version)));
            AssetBundleManifest allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            string[] bundles = allManifest.GetAllAssetBundles();
            assetBundle.Unload(false);
            CopyBundle(bundles, GetOutPath(buildTarget), targer, true);
            CopyFile(AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget), GetOutPath(buildTarget), targer, true);
            CopyFile("version.txt", GetOutPath(buildTarget), targer, true);
            EncryptedBundle(bundles, allManifest, Path.Combine(targer, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget)));
            AssetDatabase.Refresh();
        }

        public static void Move2Package(string version, BuildTarget buildTarget, string packagePath)
        {
            if (!System.IO.Directory.Exists(Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget))))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget)));
            }
            if (!System.IO.Directory.Exists(Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget) + "_Data")))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget) + "_Data"));
            }
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(GetOutDataPath(buildTarget), HDResolver.GetManifestName(version)));
            AssetBundleManifest allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            assetBundle.Unload(false);
            string targerManifestPath = Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget), AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget));
            string[] bundles = null;
            if (File.Exists(targerManifestPath))
            {
                AssetBundle packageBundle = AssetBundle.LoadFromFile(targerManifestPath);
                AssetBundleManifest packageManifest = packageBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                packageBundle.Unload(false);
                List<string> changedBundle = new List<string>();
                foreach (var bundle in allManifest.GetAllAssetBundles())
                {
                    if (allManifest.GetAssetBundleHash(bundle) != packageManifest.GetAssetBundleHash(bundle))
                        changedBundle.Add(bundle);
                }
                bundles = changedBundle.ToArray();
            }
            else
            {
                bundles = allManifest.GetAllAssetBundles();
            }
            // 复制Bundle
            CopyBundle(bundles, GetOutPath(buildTarget), packagePath, true);
            CopyFile(AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget), GetOutPath(buildTarget), packagePath, true);
            CopyFile("version.txt", GetOutPath(buildTarget), packagePath, true);
            EncryptedBundle(bundles, allManifest, Path.Combine(packagePath, AssetBundlePathResolver.GetBundlePlatformOutput(buildTarget)));
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
            System.GC.Collect();
            AssetDatabase.Refresh();
        }

        public static void CreateAssetVersion(BuildTarget buildTarget, string version)
        {
            string outPath = Path.Combine(GetOutPath(buildTarget), "version.txt");
            HDResolver.WriteFile(outPath, version);
        }

        static void CopyBundle(string[] bundles, string srcdir, string dstdir, bool overwrite)
        {
            string todir = Path.Combine(dstdir, Path.GetFileName(srcdir));
            if (!Directory.Exists(todir))
                Directory.CreateDirectory(todir);
            foreach (var bundle in bundles)
            {
                File.Copy(Path.Combine(srcdir, Path.GetFileName(bundle)), Path.Combine(todir, Path.GetFileName(bundle)), overwrite);
            }
        }

        static void CopyFile(string file, string srcdir, string dstdir, bool overwrite)
        {
            string todir = Path.Combine(dstdir, Path.GetFileName(srcdir));
            if (!Directory.Exists(todir))
                Directory.CreateDirectory(todir);
            File.Copy(Path.Combine(srcdir, Path.GetFileName(file)), Path.Combine(todir, Path.GetFileName(file)), overwrite);
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

        static void GenerateBinaryAsset(ABPackage[] packages)
        {
            foreach (var package in packages)
            {
                if (package.binary)
                {
                    int index = 0;
                    int max = package.assets.Count;
                    string binaryDataPath = GetBinaryDataPath(package.packageName);
                    if (System.IO.File.Exists(binaryDataPath))
                        System.IO.File.Delete(binaryDataPath);
                    foreach (var asset in package.assets)
                    {
                        EditorUtility.DisplayCancelableProgressBar("[GenerateBinaryAsset]    " + package.packageName, asset, (float)index++ / max);
                        string binaryPath = asset + ".bytes";
                        System.IO.File.Copy(asset, binaryPath, true);
                        string sourcePath = new System.IO.FileInfo(asset).FullName.Replace("\\", "/").Replace(Application.dataPath + "/", "");
                        using (StreamWriter sw = new StreamWriter(binaryDataPath, true))
                        {
                            sw.WriteLine(sourcePath);
                            sw.Flush();
                            sw.Close();
                        }
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        static void DeleteBinaryAsset(ABPackage[] packages)
        {
            foreach (var package in packages)
            {
                if (package.binary)
                {
                    foreach (var asset in package.assets)
                    {
                        System.IO.File.Delete(asset + ".bytes");
                    }
                    string binaryDataPath = GetBinaryDataPath(package.packageName);
                    System.IO.File.Delete(binaryDataPath);
                }
            }
            AssetDatabase.Refresh();
        }

        static string GetBinaryDataPath(string packageName)
        {
            return Application.dataPath + "/" + Path.GetFileNameWithoutExtension(packageName) + "_binaryData.txt";
        }

        static void EncryptedBundle(string[] bundles, AssetBundleManifest manifest, string path)
        {
            byte[] sourceBytes = new byte[1048576];
            foreach (var bundle in bundles)
            {
                string newBundlePath = Path.Combine(path, bundle.Replace(Path.GetFileNameWithoutExtension(bundle), manifest.GetAssetBundleHash(bundle).ToString()));
                File.Move(Path.Combine(path, Path.GetFileName(bundle)), newBundlePath);
                ulong offset = HDResolver.BundleOffset(bundle);
                using (var fileStream = new FileStream(newBundlePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    int sourceLengh = sourceBytes.Length;
                    while (sourceLengh < fileStream.Length)
                    {
                        sourceLengh = sourceLengh * 2;
                    }
                    if (sourceBytes.Length < sourceLengh)
                    {
                        sourceBytes = new byte[sourceLengh];
                    }
                    fileStream.Read(sourceBytes, 0, (int)fileStream.Length);
                    fileStream.Seek((int)offset, SeekOrigin.Begin);
                    fileStream.Write(sourceBytes, 0, (int)fileStream.Length);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
        }

    }
}