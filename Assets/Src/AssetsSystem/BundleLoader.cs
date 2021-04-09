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
            rules = AssetBundleBuildConfig.GetRules(AssetBundlePathResolver.instance.GetBundleFileRuntime("bundleRule.txt"));
        }



        public override Object Load(string path)
        {
            string packagePath = GetPackageName(path);
            string[] deps = allManifest.GetDirectDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath).Load(path);
        }

        public override Object[] LoadAll(string packagePath)
        {
            string[] deps = allManifest.GetDirectDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath).LoadAll();
        }

        public override void LoadAsync(string path, Action<Object> callback)
        {
            string packagePath = GetPackageName(path);
            string[] deps = allManifest.GetDirectDependencies(packagePath);
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
            string[] deps = allManifest.GetDirectDependencies(packagePath);
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

        public override bool LoadAllRefPackage(string packagePath)
        {
            string[] deps = allManifest.GetDirectDependencies(packagePath);
            foreach (var dep in deps)
            {
                (LoadPackage(dep) as BundlePackage).AddPackageRef(packagePath);
            }
            return LoadPackage(packagePath) != null;
        }

        public override void LoadAllRefPackageAsync(string packagePath, Action<bool> callback)
        {
            string[] deps = allManifest.GetDirectDependencies(packagePath);
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

        public override void Unload(string path)
        {
            string packagePath = GetPackageName(path);
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
                Debug.LogWarning("Bundle System Error: Not find Package by Obj:" + obj.name);
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
                Debug.LogWarning("Bundle System Error: Not Find Package:" + packagePath);
            }
        }

        public override void Destory()
        {
            base.Destory();
        }

        public override string GetPackageName(string path)
        {
            string fullPath = CombinePath(root, path);
            //如果路径不含有后缀, 自动填入一个任何后缀格式
            if (!System.Text.RegularExpressions.Regex.IsMatch(path, @".+\..+$"))
                fullPath = fullPath + ".*";
            // 根据需求可以考虑缓存
            foreach (var rule in rules)
            {
                var matchInfo = AssetBundleBuildConfig.MatchAssets(fullPath, rule);
                if (matchInfo != null)
                {
                    return matchInfo.packName;
                }
            }
            Debug.LogError("GetPackageName Fall:" + path);
            return string.Empty;
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
                string[] deps = allManifest.GetDirectDependencies(packagePath);
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

    }
}