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
            m_Loader.Initialize(root.ToLower());
        }

        void OnDestory()
        {
            m_Loader.Destory();
        }

        public Object Load(string path)
        {
            return m_Loader.Load(path.ToLower());
        }


        public T Load<T>(string path)
        where T : Object
        {
            return Load(path) as T;
        }


        public Object[] LoadAll(string path)
        {
            return m_Loader.LoadAll(path.ToLower());
        }


        public T[] LoadAll<T>(string path)
        where T : Object
        {
            return Array.ConvertAll<Object, T>(LoadAll(path), s => s as T);
        }


        public void LoadAsync(string path, Action<Object> callback)
        {
            m_Loader.LoadAsync(path.ToLower(), (obj) =>
            {
                callback.Invoke(obj);
            });
        }


        public void LoadAsync<T>(string path, Action<T> callback)
        where T : Object
        {
            LoadAsync(path.ToLower(), (obj) =>
            {
                callback(obj as T);
            });
        }

        public void LoadAllAsync(string path, Action<Object[]> callback)
        {
            m_Loader.LoadAllAsync(path.ToLower(), (obj) =>
            {
                callback(obj);
            });
        }

        public void LoadAllAsync<T>(string path, Action<T[]> callback)
        where T : Object
        {
            m_Loader.LoadAllAsync(path.ToLower(), (obj) =>
            {
                callback(Array.ConvertAll<Object, T>(obj, s => s as T));
            });
        }

        public bool LoadPackage(string packagePath)
        {
            return m_Loader.LoadAllRefPackage(packagePath.ToLower());
        }

        public void LoadPackageAsync(string packagePath, Action<bool> callback)
        {
            m_Loader.LoadAllRefPackageAsync(packagePath.ToLower(), callback);
        }

        public string LoadScene(string scenePath)
        {
            scenePath = scenePath.ToLower();
            string scenePackage;
            if (!m_Loader.Path2Package(scenePath, out scenePackage))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + scenePath);
                return string.Empty;
            }
            m_Loader.LoadAllRefPackage(scenePackage);
            return GetSceneNameByPath(scenePath);
        }

        public void LoadSceneAsync(string scenePath, Action<string> callback)
        {
            scenePath = scenePath.ToLower();
            string scenePackage;
            if (!m_Loader.Path2Package(scenePath, out scenePackage))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + scenePath);
                callback?.Invoke(string.Empty);
                return;
            }
            m_Loader.LoadAllRefPackageAsync(scenePackage, (ok) =>
            {
                callback?.Invoke(GetSceneNameByPath(scenePath));
                if (!ok)
                {
                    Debug.LogError("Not find Scene name:" + scenePath);
                }
            });
        }

        public bool ExistAsset(string path)
        {
            return m_Loader.ExistAsset(path.ToLower());
        }

        public void Unload(string path)
        {
            m_Loader.Unload(path.ToLower());
        }


        public void Unload(Object obj)
        {
            m_Loader.Unload(obj);
        }


        public void UnloadAll(string path)
        {
            m_Loader.UnloadAll(path.ToLower());
        }

        public void UnloadScene(string scenePath)
        {
            scenePath = scenePath.ToLower();
            string scenePackage;
            if (!m_Loader.Path2Package(scenePath, out scenePackage))
            {
                Debug.LogError("AssetSystem  Assetpath to Package Error:" + scenePath);
                return;
            }
            m_Loader.UnloadAll(scenePackage);
        }

        private string GetSceneNameByPath(string scenePath)
        {
            return System.Text.RegularExpressions.Regex.Replace(scenePath, @".+/", "");
        }

    }
}