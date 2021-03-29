using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = UnityEngine.Object;
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


    public void Initialize(string root, LoadType _loadType)
    {
        m_loadType = _loadType;
        switch (m_loadType)
        {
            case LoadType.AssetBundle:
                m_Loader = new BundleLoader();
                break;
            case LoadType.Resource:
                break;
            case LoadType.IO:
                break;
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
        //AssetBundle.LoadFromFile(@"D:\UnityProjects\mobileframework\Bundles\fffff_asset");
        // AssetBundle ab = AssetBundle.LoadFromFileAsync(path).completed(() =>
        // {

        // });
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