using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;
using AssetSystem;
namespace AssetSystem
{
    internal class BundleLoader : BaseAssetLoader
    {
        public static AssetBundleManifest allManifest;
        private AssetBundleRule[] rules;
        public override void Initialize(string root)
        {
            base.Initialize(root);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
            allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            foreach (var item in allManifest.GetAllAssetBundles())
            {
                Debug.Log("init bundle:" + item);
            }
            rules = AssetBundleBuildConfig.GetRules(AssetBundlePathResolver.instance.GetBundleFileRuntime("bundleRule.txt"));
            foreach (var rule in rules)
            {
                Debug.Log("init rule:" + rule.expression + " => " + rule.packName);
            }
        }



        public override Object Load(string path)
        {
            string packagePath = GetPackageName(CombinePath(root, path));
            string[] deps = allManifest.GetAllDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath).Load(path);
        }

        public override Object[] LoadAll(string packagePath)
        {
            string[] deps = allManifest.GetAllDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath).LoadAll();
        }

        public override void LoadAsync(string path, Action<Object> callback)
        {
            string packagePath = GetPackageName(CombinePath(root, path));
            string[] deps = allManifest.GetAllDependencies(packagePath);
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

        public override void LoadAllAsync(string packagePath, Action<Object[]> callback)
        {
            packagePath = CombinePath(root, packagePath);
            string[] deps = allManifest.GetAllDependencies(packagePath);
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

        public override void Unload(string path)
        {
            string packagePath = GetPackageName(CombinePath(root, path));
            if (packMapping.ContainsKey(packagePath))
            {
                LoadPackage(packagePath).Unload(path);
                DestoryNoneRefPackage(packagePath);
            }
            else
            {
                Debug.LogError("Bundle System Error: Not Find Package:" + packagePath);
            }
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
            else
            {
                Debug.LogError("Bundle System Error: Not find Package by Obj:" + obj.name);
            }
        }

        public override void UnloadAll(string packagePath)
        {
            if (packMapping.ContainsKey(packagePath))
            {
                LoadPackage(packagePath).UnloadAll();
                DestoryNoneRefPackage(packagePath);
            }
            else
            {
                Debug.LogError("Bundle System Error: Not Find Package:" + packagePath);
            }
        }

        public override void Destory()
        {
            base.Destory();
        }

        protected override string GetPackageName(string path)
        {
            // 根据需求可以考虑缓存
            foreach (var rule in rules)
            {
                var info = AssetBundleBuildConfig.MatchAssets(path, rule);
                if (info != null)
                {
                    return info.packName;
                }
            }
            Debug.LogError("path not rule package:" + path);
            return path;
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
                string[] deps = allManifest.GetAllDependencies(packagePath);
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
                if (!immortal)
                {
                    UnloadPackage(packagePath);
                }
            }
        }

    }
}