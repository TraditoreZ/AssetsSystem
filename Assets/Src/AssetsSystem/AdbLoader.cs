#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;
namespace AssetSystem
{
    public class AdbLoader : BaseAssetLoader
    {
        // AssetDatabase用于编辑器预览
        public override void Initialize(string root)
        {
            base.Initialize(root);
        }

        public override void Destory()
        {
            base.Destory();
        }

        public override Object Load(string path)
        {
            path = CombinePath(root, path);
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                return null;
            }
            return LoadPackage(packagePath).Load(path);
        }

        public override Object[] LoadAll(string packagePath)
        {
            packagePath = CombinePath(root, packagePath);
            return LoadPackage(packagePath).LoadAll();
        }


        public override void LoadAsync(string path, Action<Object> callback)
        {
            path = CombinePath(root, path);
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                callback?.Invoke(null);
                return;
            }
            LoadPackage(packagePath).LoadAsync(path, callback);
        }

        public override void LoadAllAsync(string packagePath, Action<Object[]> callback)
        {
            packagePath = CombinePath(root, packagePath);
            LoadPackage(packagePath).LoadAllAsync(callback);
        }

        public override void Unload(string path)
        {
            path = CombinePath(root, path);
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                return;
            }
            if (packMapping.ContainsKey(packagePath))
            {
                IAssetPackage package = LoadPackage(packagePath);
                package.Unload(path);
                if (package.LoadCount() == 0)
                {
                    UnloadPackage(packagePath);
                }
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
                if (package.LoadCount() == 0)
                {
                    UnloadPackage(package.PackagePath());
                }
            }
        }

        public override void UnloadAll(string packagePath)
        {
            packagePath = CombinePath(root, packagePath);
            if (packMapping.ContainsKey(packagePath))
            {
                LoadPackage(packagePath).UnloadAll();
                UnloadPackage(packagePath);
            }
        }

        public override bool Path2Package(string path, out string packageName)
        {
            packageName = path.Substring(0, path.LastIndexOf('/'));
            return File.Exists(packageName);
        }

        public override bool ExistAsset(string path)
        {
            path = CombinePath(root, path);
            string packagePath;
            if (!Path2Package(path, out packagePath))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + path);
                return false;
            }
            return LoadPackage(packagePath).Exist(path);
        }

        protected override IAssetPackage CreatePackage() { return AdbPackage.CreateObject(); }

        protected override void DestoryPackage(IAssetPackage package) { AdbPackage.ReclaimObject(package as AdbPackage); }

        private string CombinePath(string p1, string p2)
        {
            return p2.Contains("assets") ? p2.Replace('\\', '/') : System.IO.Path.Combine(p1, p2).Replace('\\', '/');
        }

    }
}
#endif