using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
public class ResourceLoader : IAssetLoader
{
    private Dictionary<string, IAssetPackage> packMapping;

    public void Initialize(string root)
    {
        packMapping = new Dictionary<string, IAssetPackage>();
    }


    public void Destory()
    {
    }


    public Object Load(string path)
    {
        return Resources.Load(path);
    }

    public Object[] LoadAll(string path)
    {
        return Resources.LoadAll(path);
    }


    public void LoadAsync(string path, Action<Object> callback)
    {
        //   Resources.LoadAsync(path)
    }

    public void LoadAllAsync(string path, Action<Object[]> callback)
    {
        throw new NotImplementedException();
    }

    public void Unload(string path)
    {

    }

    public void Unload(Object obj)
    {
        throw new NotImplementedException();
    }

    public void UnloadAll(string path)
    {

    }
}
