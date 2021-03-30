using System;
using Object = UnityEngine.Object;

internal interface IAssetLoader
{
    void Initialize(string root);
    void Destory();
    // IAssetPackage LoadPackage(string packagePath, bool asyncLoad);
    // void UnloadPackage(string packagePath);

    Object Load(string path);
    Object[] LoadAll(string packagePath);
    void LoadAsync(string path, Action<Object> callback);
    void LoadAllAsync(string path, Action<Object[]> callback);
    void Unload(string path);
    void Unload(Object obj);
    void UnloadAll(string packagePath);
    bool IsPackageCreated(string path);
    string GetPackageName(string path);
}