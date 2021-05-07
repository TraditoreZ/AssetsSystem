using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetEditor
{
    public class EnumMultiAttribute : PropertyAttribute { }

    /// <summary>
    /// 绘制多选属性
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumMultiAttribute))]
    public class EnumMultiAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MethodInfo miIntToEnumFlags = typeof(EditorGUI).GetMethod("IntToEnumFlags", BindingFlags.Static | BindingFlags.NonPublic);
            Enum currentEnum = miIntToEnumFlags.Invoke(null, new object[] { fieldInfo.FieldType, property.intValue }) as Enum;
            Enum newEnum = EditorGUI.EnumFlagsField(position, label, currentEnum);
            property.intValue = Convert.ToInt32(newEnum);
        }
    }


}