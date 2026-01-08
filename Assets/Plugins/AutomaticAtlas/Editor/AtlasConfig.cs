using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class AtlasConfig : ScriptableObject
{
    [Tooltip("图集输出目录（Unity 项目内路径，例如 Assets/Art/Atlas）")]
    public string outputDirectory;

    [Tooltip("图集打包方式：按文件夹 / 按依赖关系")]
    public EPackType packType = EPackType.Folder;

    [Tooltip("用于检测依赖关系的资源路径（通常是预制体目录）")]
    public string checkAssetsPath;

    [Tooltip("单张图集的最大尺寸（单位：像素）")]
    public int maxSize = 1024;

    [Tooltip("公共图集名称（根据预制体依赖生成）")]
    public string atlasName_Comm = "Common";

    [Tooltip("大尺寸公共图集名称（根据预制体依赖生成）")]
    public string atlasName_Comm_big = "Common_big";

    [Tooltip("用于按文件夹打图集的贴图根目录")]
    public string checkTexturesPath;

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