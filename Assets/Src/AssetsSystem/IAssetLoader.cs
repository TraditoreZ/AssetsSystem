using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Linq;

namespace AssetSystem
{
    internal interface IAssetLoader
    {
        void Initialize(string root);
        void Destory();
        Object Load(string path);
        Object[] LoadAll(string packagePath);
        void LoadAsync(string path, Action<Object> callback);
        void LoadAllAsync(string path, Action<Object[]> callback);
        bool LoadAllRefPackage(string packagePath);
        void LoadAllRefPackageAsync(string packagePath, Action<bool> callback);
        void Unload(string path);
        void Unload(Object obj);
        void UnloadAll(string packagePath);
        string GetPackageName(string path);
    }



    public abstract class BaseAssetLoader : IAssetLoader
    {
        protected string root;
        protected Dictionary<string, IAssetPackage> packMapping;
        public virtual void Initialize(string root)
        {
            this.root = root;
            packMapping = new Dictionary<string, IAssetPackage>(256);
        }

        public virtual void Destory()
        {
            var packagePaths = packMapping.Keys.ToArray();
            foreach (var path in packagePaths)
                UnloadPackage(path);
            packMapping.Clear();
            packMapping = null;
            root = null;
        }


        public abstract Object Load(string path);

        public abstract Object[] LoadAll(string path);


        public abstract void LoadAsync(string path, Action<Object> callback);


        public abstract void LoadAllAsync(string path, Action<Object[]> callback);


        public abstract void Unload(string path);


        public abstract void Unload(Object obj);


        public abstract void UnloadAll(string packagePath);

        public virtual bool LoadAllRefPackage(string packagePath)
        {
            return LoadPackage(packagePath) != null;
        }

        public virtual void LoadAllRefPackageAsync(string packagePath, Action<bool> callback)
        {
            LoadPackageAsync(packagePath, (package) =>
            {
                callback?.Invoke(package != null);
            });
        }

        protected IAssetPackage LoadPackage(string packagePath)
        {
            IAssetPackage package;
            if (!packMapping.TryGetValue(packagePath, out package))
            {
                package = CreatePackage();
                packMapping.Add(packagePath, package);
                package.LoadPackage(packagePath, false);
            }
            return package;
        }

        protected void LoadPackageAsync(string packagePath, Action<IAssetPackage> call)
        {
            IAssetPackage package;
            if (!packMapping.TryGetValue(packagePath, out package))
            {
                package = CreatePackage();
                packMapping.Add(packagePath, package);
                package.LoadPackage(packagePath, true);
            }
            if (package.PackageLoaded())
            {
                call?.Invoke(package);
            }
            else
            {
                package.OnPackageLoaded(call);
            }
        }

        protected void UnloadPackage(string packagePath)
        {
            IAssetPackage package;
            if (packMapping.TryGetValue(packagePath, out package))
            {
                packMapping.Remove(packagePath);
                package.UnloadPackage();
                DestoryPackage(package);
            }
        }

        protected abstract IAssetPackage CreatePackage();

        protected abstract void DestoryPackage(IAssetPackage package);

        public abstract string GetPackageName(string path);

    }
}