using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class BundleLoader : IAssetLoader
{
    public void Destory()
    {
        throw new NotImplementedException();
    }

    public void Initialize(string root)
    {
        throw new NotImplementedException();
    }

    public UnityEngine.Object Load(string path)
    {
        throw new NotImplementedException();
    }

    public UnityEngine.Object[] LoadAll(string path)
    {
        throw new NotImplementedException();
    }

    public void LoadAllAsync(string path, Action<UnityEngine.Object[]> callback)
    {
        throw new NotImplementedException();
    }

    public void LoadAsync(string path, Action<UnityEngine.Object> callback)
    {

    }

    public void Unload(string path)
    {
        throw new NotImplementedException();
    }

    public void Unload(UnityEngine.Object obj)
    {
        throw new NotImplementedException();
    }

    public void UnloadAll(string path)
    {
        throw new NotImplementedException();
    }
}
