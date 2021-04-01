using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Linq;
namespace TF.AssetEditor
{
    public class AssetBuilder : Editor
    {
        //====================================================================================================================
        const string configPath = "Assets/Src/AssetsSystem/Editor/exampleCfg";
        const string outputPath = "./Bundles";
        const BuildAssetBundleOptions options = BuildAssetBundleOptions.UncompressedAssetBundle;//BuildAssetBundleOptions.ChunkBasedCompression;
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



        public static void BuildAssetBundle(BuildTarget buildTarget = BuildTarget.StandaloneWindows64)
        {
            try
            {
                if (!System.IO.Directory.Exists(outputPath))
                {
                    System.IO.Directory.CreateDirectory(outputPath);
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

                // 如果运行时采用正则表达式 那么可以不需要这张表了
                //CreateABConfig(packsDic);
                //根据BuildSetting里面所激活的平台进行打包 设置过AssetBundleName的都会进行打包  
                //BuildPipeline.BuildAssetBundles(outputPath, options, buildTarget);
                BuildPipeline.BuildAssetBundles(outputPath, abbLists.ToArray(), options, buildTarget);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
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

        static void CreateABConfig(Dictionary<string, ABPackage> packsDic)
        {
            AssetSystem.AssetConfig assetCfg = new AssetSystem.AssetConfig();
            foreach (var pack in packsDic.Values)
            {
                string options = string.Join("", pack.options);
                assetCfg.packInfos.Add(pack.packageName, options);
                foreach (var path in pack.assets)
                {
                    assetCfg.assetMaps.Add(path, pack.packageName);
                }
            }
            FileStream fs = new FileStream(outputPath + "/AssetBundleConf.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(JsonUtility.ToJson(assetCfg));
            sw.Flush();
            sw.Close();
            fs.Close();
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

    }
}