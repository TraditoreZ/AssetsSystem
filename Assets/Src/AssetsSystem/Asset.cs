using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetSystem
{
    public static class Asset
    {
        private static AssetSystemCore core { get { return AssetSystemCore.Instance; } }

        public static void Initialize(string root, LoadType _loadType, bool simulateIODelay = false)
        {
            core.Initialize(root, _loadType, simulateIODelay);
        }

        public static Object Load(string path)
        {
            return core.Load(path);
        }

        public static T Load<T>(string path)
        where T : Object
        {
            return core.Load<T>(path);
        }

        public static Object[] LoadAll(string path)
        {
            return core.LoadAll(path);
        }

        public static T[] LoadAll<T>(string path)
        where T : Object
        {
            return core.LoadAll<T>(path);
        }

        public static void LoadAsync(string path, Action<Object> callback)
        {
            core.LoadAsync(path, callback);
        }

        public static void LoadAsync<T>(string path, Action<T> callback)
        where T : Object
        {
            core.LoadAsync(path, callback);
        }

        public static void LoadAllAsync(string path, Action<Object[]> callback)
        {
            core.LoadAllAsync(path, callback);
        }

        public static void LoadAllAsync<T>(string path, Action<T[]> callback)
        where T : Object
        {
            core.LoadAllAsync<T>(path, callback);
        }

        public static bool LoadPackage(string packagePath)
        {
            return core.LoadPackage(packagePath);
        }

        public static void LoadPackageAsync(string packagePath, Action<bool> callback)
        {
            core.LoadPackageAsync(packagePath, callback);
        }

        public static void Unload(string path)
        {
            core.Unload(path);
        }

        public static void Unload(Object obj)
        {
            core.Unload(obj);
        }

        public static void UnloadAll(string path)
        {
            core.UnloadAll(path);
        }

    }
}