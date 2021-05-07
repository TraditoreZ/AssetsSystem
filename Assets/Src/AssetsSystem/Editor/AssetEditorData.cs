using System.Collections;
using System.Collections.Generic;
using AssetEditor;
using UnityEditor;
using UnityEngine;

// [CreateAssetMenu(menuName = "MySubMenue/Create MyScriptableObject ")]
public class AssetEditorData : ScriptableObject
{
    public string configPath = "Assets/Src/AssetsSystem/Editor/exampleCfg";

    [EnumMultiAttribute]
    public BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;

    public BuildTarget lastBuildTarger;
    public bool increment;
    public string incrementPath;
    public string version;
    public string sourceVersion;
}