using System;
using UnityEngine;
using Object = UnityEngine.Object;
internal interface IAssetLoader
{
    void Initialize(string root);
    Object Load(string path);
    Object[] LoadAll(string path);
    void LoadAsync(string path, Action<Object> callback);
    void LoadAllAsync(string path, Action<Object[]> callback);
    void Unload(string path);
    void Unload(Object obj);
    void UnloadAll(string path);
    void Destory();
}