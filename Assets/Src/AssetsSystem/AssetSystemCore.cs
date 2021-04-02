using System.Collections;
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




    }
}