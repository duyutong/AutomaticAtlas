using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class AtlasConfig : ScriptableObject
{
    public string checkAssetsPath;
    public string outputDirectory;
    public string atlasName_Comm = "Common";
    public string atlasName_Comm_big = "Common_big";
    public int maxSize = 2048;

    // 在编辑器中创建资源的菜单项
    [MenuItem("Assets/Create/Create AtlasConfig")]
    public static void CreateAsset()
    {
        // 创建一个新的资源
        AtlasConfig newAsset = CreateInstance<AtlasConfig>();

        // 指定资源的保存路径
        string assetPath = AutomaticAtlas.configPath;

        // 创建资源并保存
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();

        // 选中新创建的资源
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newAsset;
    }
}