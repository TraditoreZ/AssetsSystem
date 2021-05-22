using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;
using AssetSystem;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssetSystem
{
    internal class BundleLoader : BaseAssetLoader
    {
        private List<AssetBundleRule> rules;
        private Dictionary<string, string[]> allDependencies;
        public override void Initialize(string root)
        {
            base.Initialize(root);
            InitConfig();
        }



        public override Object Load(string path)
        {
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                return null;
            }
            string[] deps = GetAllDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath).Load(path);
        }

        public override Object[] LoadAll(string packagePath)
        {
            string[] deps = GetAllDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath).LoadAll();
        }

        public override void LoadAsync(string path, Action<Object> callback)
        {
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                callback?.Invoke(null);
                return;
            }
            string[] deps = GetAllDependencies(packagePath);
            if (deps.Length > 0)
            {
                int currtDep = 0;
                foreach (var dep in deps)
                {
                    LoadPackageAsync(dep, (depPackage) =>
                    {
                        (depPackage as BundlePackage).AddPackageRef(packagePath);
                        if (++currtDep == deps.Length)
                        {
                            LoadPackageAsync(packagePath, (package) =>
                            {
                                package.LoadAsync(path, callback);
                            });
                        }
                    });
                }
            }
            else
            {
                LoadPackageAsync(packagePath, (package) =>
                {
                    package.LoadAsync(path, callback);
                });
            }
        }

        public override void LoadAllAsync(string packagePath, Action<Object[]> callback)
        {
            string[] deps = GetAllDependencies(packagePath);
            if (deps.Length > 0)
            {
                int currtDep = 0;
                foreach (var dep in deps)
                {
                    LoadPackageAsync(dep, (depPackage) =>
                    {
                        (depPackage as BundlePackage).AddPackageRef(packagePath);
                        if (++currtDep == deps.Length)
                        {
                            LoadPackageAsync(packagePath, (package) =>
                            {
                                package.LoadAllAsync(callback);
                            });
                        }
                    });
                }
            }
            else
            {
                LoadPackageAsync(packagePath, (package) =>
                {
                    package.LoadAllAsync(callback);
                });
            }
        }

        public override bool LoadAllRefPackage(string packagePath)
        {
            string[] deps = GetAllDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath) != null;
        }

        public override void LoadAllRefPackageAsync(string packagePath, Action<bool> callback)
        {
            string[] deps = GetAllDependencies(packagePath);
            if (deps.Length > 0)
            {
                int currtDep = 0;
                foreach (var dep in deps)
                {
                    LoadPackageAsync(dep, (depPackage) =>
                    {
                        (depPackage as BundlePackage).AddPackageRef(packagePath);
                        if (++currtDep == deps.Length)
                        {
                            LoadPackageAsync(packagePath, (package) =>
                            {
                                callback?.Invoke(package != null);
                            });
                        }
                    });
                }
            }
            else
            {
                LoadPackageAsync(packagePath, (package) =>
                {
                    callback?.Invoke(package != null);
                });
            }
        }

        public override void Unload(string path)
        {
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                return;
            }
            if (packMapping.ContainsKey(packagePath))
            {
                LoadPackage(packagePath).Unload(path);
                DestoryNoneRefPackage(packagePath);
            }
            // else
            // {
            //     Debug.LogError("Unload Error   path:" + path);
            // }
        }

        public override void Unload(Object obj)
        {
            IAssetPackage package = null;
            foreach (var tempPack in packMapping.Values)
            {
                if (tempPack.IsLoaded(obj.name))
                {
                    package = tempPack;
                }
            }
            if (package != null)
            {
                package.Unload(obj);
                DestoryNoneRefPackage(package.PackagePath());
            }
            // else
            // {
            //     Debug.LogWarning("Bundle System Error: Not find Package by Obj:" + obj.name);
            // }
        }

        public override void UnloadAll(string packagePath)
        {
            if (packMapping.ContainsKey(packagePath))
            {
                LoadPackage(packagePath).UnloadAll();
                DestoryNoneRefPackage(packagePath);
            }
            // else
            // {
            //     Debug.LogWarning("Bundle System Error: Not Find Package:" + packagePath);
            // }
        }

        public override void Destory()
        {
            base.Destory();
        }

        public override bool Path2Package(string path, out string packageName)
        {
            packageName = string.Empty;
            //如果路径包含Asset根目录， 则不进行根目录合并
            string fullPath = path.IndexOf("assets/") >= 0 ? path : CombinePath(root, path);
            foreach (var rule in rules)
            {
                var matchInfo = AssetBundleBuildConfig.MatchAssets(fullPath, rule);
                if (matchInfo.HasValue)
                {
                    packageName = matchInfo.Value.packName;
                    return true;
                }
            }
            Debug.LogError("GetPackageName Fall:" + path);
            return false;
        }

        public override void OnAssetsUpdate()
        {
            base.OnAssetsUpdate();
            InitConfig();
        }

        private string CombinePath(string p1, string p2)
        {
            return System.IO.Path.Combine(p1, p2).Replace('\\', '/');
        }

        protected override IAssetPackage CreatePackage() { return BundlePackage.CreateObject(); }

        protected override void DestoryPackage(IAssetPackage package) { BundlePackage.ReclaimObject(package as BundlePackage); }


        private void DestoryNoneRefPackage(string packagePath)
        {
            BundlePackage rp = LoadPackage(packagePath) as BundlePackage;
            if (!rp.CheckSelfRef())
            {
                string[] deps = GetAllDependencies(packagePath);
                foreach (var dep in deps)
                {
                    if (packMapping.ContainsKey(dep))
                    {
                        BundlePackage depPackage = LoadPackage(dep) as BundlePackage;
                        if (depPackage.RemovePackageRef(packagePath))
                        {
                            DestoryNoneRefPackage(dep);
                        }
                    }
                }
                bool immortal = false;  // 重要的包不卸载
                foreach (var rule in rules)
                {
                    if (rule.packName == packagePath)
                    {
                        immortal = System.Array.IndexOf<string>(rule.options, "immortal") >= 0;
                    }
                }
                if (immortal)
                {
                    Debug.Log("[immortal] " + packagePath + " is immortal, not unload");
                }
                if (!immortal)
                {
                    UnloadPackage(packagePath);
                }
            }
        }

        private readonly string[] emptyArray = new string[] { };

        private string[] GetAllDependencies(string assetBundleName)
        {
            string[] values;
            if (allDependencies.TryGetValue(assetBundleName, out values))
            {
                return values;
            }
            else
            {
                return emptyArray;
            }
        }

        private void InitConfig()
        {
            try
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
                AssetBundleManifest allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                allDependencies = new Dictionary<string, string[]>();
                var bundles = allManifest.GetAllAssetBundles();
                foreach (var bundle in bundles)
                {
                    allDependencies.Add(bundle, allManifest.GetAllDependencies(bundle));
                }
                assetBundle.Unload(true);
                AssetBundle ruleAB = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime("bundle.rule"));
                string[] commands = ruleAB.LoadAllAssets<TextAsset>().FirstOrDefault().text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                ruleAB.Unload(true);
                rules = new List<AssetBundleRule>();
                AssetBundleBuildConfig.ResolveRule(rules, commands);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

        }

    }
}