using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

internal class BundleLoader : IAssetLoader
{
    public void Destory()
    {
        throw new NotImplementedException();
    }

    public string GetPackageName(string path)
    {
        throw new NotImplementedException();
    }

    public void Initialize(string root)
    {
        throw new NotImplementedException();
    }

    public bool IsPackageCreated(string path)
    {
        throw new NotImplementedException();
    }

    public Object Load(string path)
    {
        throw new NotImplementedException();
    }

    public Object[] LoadAll(string packagePath)
    {
        throw new NotImplementedException();
    }

    public void LoadAllAsync(string path, Action<Object[]> callback)
    {
        throw new NotImplementedException();
    }

    public void LoadAsync(string path, Action<Object> callback)
    {
        throw new NotImplementedException();
    }

    public void Unload(string path)
    {
        throw new NotImplementedException();
    }

    public void Unload(Object obj)
    {
        throw new NotImplementedException();
    }

    public void UnloadAll(string packagePath)
    {
        throw new NotImplementedException();
    }
}
