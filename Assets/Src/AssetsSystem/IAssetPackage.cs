using System;
using Object = UnityEngine.Object;
using UnityEngine;
using System.Collections.Generic;

namespace AssetSystem
{

    public interface IAssetPackage
    {
        string PackagePath();
        bool PackageLoaded();
        void LoadPackage(string packagePath, bool async, Action<IAssetPackage> callBack = null);
        void UnloadPackage();
        void OnPackageLoaded(Action<IAssetPackage> callBack);
        Object Load(string path);
        Object[] LoadAll();
        void LoadAsync(string path, Action<Object> callback);
        void LoadAllAsync(Action<Object[]> callback);
        void Unload(string path);
        void Unload(Object obj);
        void UnloadAll();
        bool IsLoaded(string path);
        int LoadCount();
    }




    public abstract class BaseAssetPackage<T> : ObjectPool<T>, IAssetPackage
    where T : BaseAssetPackage<T>, new()
    {
        protected string packagePath;
        protected Dictionary<string, Object> assetMapping = new Dictionary<string, Object>();
        protected Queue<Action<IAssetPackage>> packageLoadedCalls = new Queue<Action<IAssetPackage>>();
        public bool IsLoaded(string path)
        {
            return assetMapping.ContainsKey(path);
        }

        public int LoadCount()
        {
            return assetMapping.Count;
        }
        public string PackagePath()
        {
            return packagePath;
        }
        public abstract bool PackageLoaded();

        public abstract Object Load(string path);

        public abstract Object[] LoadAll();

        public abstract void LoadAsync(string path, Action<Object> callback);

        public abstract void LoadAllAsync(Action<Object[]> callback);

        public virtual void LoadPackage(string packagePath, bool async, Action<IAssetPackage> callBack = null)
        {
            this.packagePath = packagePath;
            Debug.Log("[Asset Package] LoadPackage:" + packagePath);
        }

        public void OnPackageLoaded(Action<IAssetPackage> callBack)
        {
            packageLoadedCalls.Enqueue(callBack);
        }

        public abstract void Unload(string path);

        public abstract void Unload(Object obj);

        public abstract void UnloadAll();

        public virtual void UnloadPackage()
        {
            Debug.Log("[Asset Package] UnloadPackage:" + packagePath);
            UnloadAll();
        }


    }
}