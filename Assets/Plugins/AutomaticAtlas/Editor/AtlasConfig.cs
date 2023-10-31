using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class AtlasConfig : ScriptableObject
{
    public string outputDirectory;
    public EPackType packType = EPackType.Folder;
    public string checkAssetsPath;
    public int maxSize = 1024;
    #region 以预制体的依赖关系为依据来打图集
    [ShowIf("@packType==EPackType.Dependent")]
    public string atlasName_Comm = "Common";

    [ShowIf("@packType==EPackType.Dependent")]
    public string atlasName_Comm_big = "Common_big";
    #endregion

    #region 以文件夹为依据打图集
    [ShowIf("@packType==EPackType.Folder")]
    public string checkTexturesPath;
    #endregion

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
public enum EPackType 
{
    Folder,
    Dependent,
}