﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace AssetSystem
{
    public class AssetSystemCore : MonoBehaviour
    {

        private IAssetLoader m_Loader;
        private LoadType m_loadType;

        private static AssetSystemCore _instance;
        public static AssetSystemCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("_AssetSystem");
                    obj.AddComponent<AssetSystemCore>();
                }
                return _instance;
            }
        }

        /// <summary> AssetDatabase模式下模拟异步IO加载延迟, 建议开发阶段开启 用于发现异步导致逻辑的BUG </summary>
        public bool SimulateIODelay
        {
            get
            {
                return m_simulateIODelay;
            }
        }
        private bool m_simulateIODelay;

        void Awake()
        {
            if (_instance != null)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(this);
        }


        public void Initialize(string root, LoadType _loadType, bool simulateIODelay = false)
        {
            m_loadType = _loadType;
            m_simulateIODelay = simulateIODelay;
            switch (m_loadType)
            {
                case LoadType.AssetBundle:
                    m_Loader = new BundleLoader();
                    break;
                case LoadType.Resource:
                    m_Loader = new ResourceLoader();
                    break;
#if UNITY_EDITOR
                case LoadType.AssetDatabase:
                    m_Loader = new AdbLoader();
                    break;
#endif
            }
            m_Loader.Initialize(root);
        }

        void OnDestory()
        {
            m_Loader.Destory();
        }

        public Object Load(string path)
        {
            return m_Loader.Load(path);
        }


        public T Load<T>(string path)
        where T : Object
        {
            return Load(path) as T;
        }


        public Object[] LoadAll(string path)
        {
            return m_Loader.LoadAll(path);
        }


        public T[] LoadAll<T>(string path)
        where T : Object
        {
            return Array.ConvertAll<Object, T>(LoadAll(path), s => s as T);
        }


        public void LoadAsync(string path, Action<Object> callback)
        {
            m_Loader.LoadAsync(path, (obj) =>
            {
                callback.Invoke(obj);
            });
        }


        public void LoadAsync<T>(string path, Action<T> callback)
        where T : Object
        {
            LoadAsync(path, (obj) =>
            {
                callback(obj as T);
            });
        }

        public void LoadAllAsync(string path, Action<Object[]> callback)
        {
            m_Loader.LoadAllAsync(path, (obj) =>
            {
                callback(obj);
            });
        }

        public void LoadAllAsync<T>(string path, Action<T[]> callback)
        where T : Object
        {
            m_Loader.LoadAllAsync(path, (obj) =>
            {
                callback(Array.ConvertAll<Object, T>(obj, s => s as T));
            });
        }

        public bool LoadPackage(string packagePath)
        {
            return m_Loader.LoadAllRefPackage(packagePath);
        }

        public void LoadPackageAsync(string packagePath, Action<bool> callback)
        {
            m_Loader.LoadAllRefPackageAsync(packagePath, callback);
        }

        public string LoadScene(string scenePath)
        {
            string scenePackage = m_Loader.GetPackageName(scenePath);
            Debug.Log(scenePackage);
            m_Loader.LoadAllRefPackage(scenePackage);
            return GetSceneNameByPath(scenePath);
        }

        public void LoadSceneAsync(string scenePath, Action<string> callback)
        {
            string scenePackage = m_Loader.GetPackageName(scenePath);
            m_Loader.LoadAllRefPackageAsync(scenePackage, (ok) =>
            {
                callback?.Invoke(GetSceneNameByPath(scenePath));
                if (!ok)
                {
                    Debug.LogError("Not find Scene name:" + scenePath);
                }
            });
        }

        public void Unload(string path)
        {
            m_Loader.Unload(path);
        }


        public void Unload(Object obj)
        {
            m_Loader.Unload(obj);
        }


        public void UnloadAll(string path)
        {
            m_Loader.UnloadAll(path);
        }

        public void UnloadScene(string scenePath)
        {
            string scenePackage = m_Loader.GetPackageName(scenePath);
            m_Loader.UnloadAll(scenePackage);
        }

        private string GetSceneNameByPath(string scenePath)
        {
            int index = scenePath.LastIndexOf('/');
            return index >= 0 ? scenePath.Substring(index + 1, scenePath.Length - index - 1) : scenePath;
        }

    }
}