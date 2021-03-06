using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetEditor
{

    public class AssetBuilderLayout : EditorWindow
    {
        [MenuItem("AssetSystem/AssetBundle")]
        public static void OpenWindows()
        {
            //开始之前进行一下检测，Streaming下有没有xml的明文文件，如果没有则生成一个
            AssetBuilderLayout window = (AssetBuilderLayout)EditorWindow.GetWindow(typeof(AssetBuilderLayout));
            // window.minSize = new Vector2(500, 850);
            // window.maxSize = new Vector2(800, 850);
            window.titleContent = new GUIContent("资源管理系统");
            //PackageLayout.Instance.GetSaveData();
            window.Show();
        }

        AssetEditorData GetAssetEditorData()
        {
            var paths = AssetDatabase.FindAssets("t:AssetEditorData");
            if (paths != null && paths.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<AssetEditorData>(AssetDatabase.GUIDToAssetPath(paths[0]));
            }
            throw new System.Exception("Not find EditorData.asset");
        }

        private AssetEditorData editorData;


        public string[] platformName = { "Windows", "Android", "IOS" };
        public int[] platformIndexs = { 5, 13, 9 };

        public string[] assetbundleMode = { "增量构建AB", "重新构建AB" };
        public string[] assetbundleType = { "构建分包", "构建母包" };

        public void OnGUI()
        {
            GUILayout.Label("【规则文件路径】");
            GUILayout.Label("【母包规则配置】");
            GUILayout.Label(editorData.baseConfigPath);
            if (GUILayout.Button("选择路径", GUILayout.Width(100), GUILayout.Height(20)))
            {
                editorData.baseConfigPath = EditorUtility.OpenFilePanel("【母包规则配置】", "", "");
            }
            GUILayout.Label("【分包规则配置】");
            GUILayout.Label(editorData.splitConfigPath);
            if (GUILayout.Button("选择路径", GUILayout.Width(100), GUILayout.Height(20)))
            {
                editorData.splitConfigPath = EditorUtility.OpenFilePanel("【分包规则配置】", "", "");
            }
            GUILayout.Label("【分包根目录】");
            GUILayout.Label(editorData.incrementPath);
            if (GUILayout.Button("选择路径", GUILayout.Width(100), GUILayout.Height(20)))
            {
                editorData.incrementPath = EditorUtility.OpenFolderPanel("【分包根目录】", "", "");
            }
            GUILayout.Label("【构建参数】");
            // MethodInfo miIntToEnumFlags = typeof(EditorGUI).GetMethod("IntToEnumFlags", BindingFlags.Static | BindingFlags.NonPublic);
            // Enum currentEnum = miIntToEnumFlags.Invoke(null, new object[] { typeof(BuildAssetBundleOptions), (int)editorData.options }) as Enum;
            // editorData.options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField(currentEnum);
            editorData.options = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("BuildAssetBundleOptions", editorData.options);

            GUILayout.Label("【平台选择】");
            editorData.lastBuildTarger = (BuildTarget)EditorGUILayout.IntPopup("", (int)editorData.lastBuildTarger,
                                    platformName, platformIndexs, GUILayout.MaxWidth(140));


            editorData.increment = EditorGUILayout.Popup("构建形式", (editorData.increment ? 0 : 1), assetbundleType) == 0;


            GUILayout.Label("【资源版本号】 " + editorData.version);
            editorData.version = EditorGUILayout.TextField("", editorData.version, GUILayout.Width(100));

            if (editorData.increment)
            {
                GUILayout.Label("【母包版本——基于母包切分】");
                editorData.sourceVersion = EditorGUILayout.TextField("", editorData.sourceVersion, GUILayout.Width(100));
            }

            if (GUILayout.Button("开始打包", GUILayout.Width(142), GUILayout.Height(30)))
            {
                AssetBuilder.BuildAssetBundle(editorData.increment ? editorData.splitConfigPath : editorData.baseConfigPath, editorData.options, editorData.lastBuildTarger);
                AssetBuilder.BuildManifest(editorData.version, editorData.lastBuildTarger);
                if (editorData.increment)
                {
                    AssetBuilder.CreateAssetVersion(editorData.lastBuildTarger, editorData.version);
                    AssetBuilder.GenerateModifyList(editorData.lastBuildTarger, editorData.version, editorData.sourceVersion);
                    AssetBuilder.Move2Package(editorData.version, editorData.lastBuildTarger, editorData.incrementPath);
                }
                else
                {
                    AssetBuilder.CreateAssetVersion(editorData.lastBuildTarger, editorData.version);
                    AssetBuilder.Move2Project(editorData.version, editorData.lastBuildTarger);
                    AssetBuilder.Move2Package(editorData.version, editorData.lastBuildTarger, editorData.incrementPath);
                }
            }
        }

        public void OnEnable()
        {
            editorData = GetAssetEditorData();
        }

        public void OnDisable()
        {
            EditorUtility.SetDirty(editorData);

        }

        public void OnDestroy()
        {
            editorData = null;
            AssetDatabase.Refresh();
        }

    }
}