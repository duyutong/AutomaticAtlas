using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Path = System.IO.Path;

public class AutomaticAtlas
{
    public static string configPath = "Assets/Plugins/AutomaticAtlas/Editor/AtlasConfig.asset";
    private static List<string> prefabPathList = new List<string>();
    private static List<string> textureFolderList = new List<string>();
    private static HashSet<string> allDependentTextureGUIDs = new HashSet<string>();
    private static AtlasConfig config;
    private static string atlasName_Comm = "Common";
    private static string atlasName_Comm_big = "Common_big";
    private static Dictionary<string, List<Sprite>> altasDic = new Dictionary<string, List<Sprite>>();
    private static Dictionary<string, TextureResInfo> repeatResDic = new Dictionary<string, TextureResInfo>();
    private static Dictionary<string, HashSet<MergeInfo>> mergeDic = new Dictionary<string, HashSet<MergeInfo>>();
    
    [MenuItem("Tools/GenerateAtlas(自动生成图集)")]
    private static void GenerateAtlas()
    {
        if (config == null) config = AssetDatabase.LoadAssetAtPath<AtlasConfig>(configPath);
        if (config.packType == EPackType.Dependent) PackByDependent();
        else if (config.packType == EPackType.Folder) PackByFolders();
    }

    private static void PackByFolders()
    {
        altasDic.Clear();
        prefabPathList.Clear();
        textureFolderList.Clear();
        allDependentTextureGUIDs.Clear();

        EditorUtilityExtensions.CheckRes(config.checkAssetsPath, ".prefab", (_path) => { prefabPathList.Add(_path); });
        SetSubdirectories(config.checkTexturesPath);
        SetAllDependentTexture();
        SetAltasDicByFolder();
        OnGenerateAtlas();

        altasDic.Clear();
        prefabPathList.Clear();
        textureFolderList.Clear();
        allDependentTextureGUIDs.Clear();
    }

    private static void SetAltasDicByFolder()
    {
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        List<string> spritePath = new List<string>();
        foreach (string _subdirectory in textureFolderList) 
        {
            spritePath.Clear();
            string folderName = Path.GetFileName(_subdirectory).Replace(" ", "_"); ;
            EditorUtilityExtensions.CheckRes(_subdirectory, ".png", (_path) =>
            {
                string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
                string currGuid = AssetDatabase.AssetPathToGUID(shortPath);
                if (!allDependentTextureGUIDs.Contains(currGuid)) return;
                spritePath.Add(_path);

                string altasName = GetAltasNameByFolder(shortPath);

                if (!altasDic.ContainsKey(altasName)) altasDic.Add(altasName, new List<Sprite>());
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(shortPath);
                altasDic[altasName].Add(AssetDatabase.LoadAssetAtPath<Sprite>(shortPath));
            });
        }
    }
    private static string GetAltasNameByFolder(string shortPath) 
    {
        string replaceStr = config.checkTexturesPath.Replace("/", @"\") + @"\";
        string temple = shortPath.Replace(replaceStr, "");
        string[] strs = temple.Split(@"\");
        string result = "";
        for (int i = 0; i < strs.Length - 1; i++) 
        {
            result += strs[i]/*.Replace(" ","_")*/+ "_";
        }
        return result.Substring(0,result.Length-1);
    }
    private static Regex regBlock = new Regex("MonoBehaviour");
    private static void SetAllDependentTexture()
    {
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            int index = i;
            string _path = prefabPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            string FolderStrInfo = File.ReadAllText(shortPath);
            string[] strArray = FolderStrInfo.Split(new string[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string blockStr in strArray)
            {
                if (regBlock.IsMatch(blockStr)) 
                {
                    Match guidMatch = Regex.Match(blockStr, "m_Sprite: {fileID: (.*), guid: (?<GuidValue>.*?), type: [0-9]}");
                    if (guidMatch.Success) 
                    {
                        string guid = guidMatch.Groups["GuidValue"].Value;
                        allDependentTextureGUIDs.Add(guid);
                    }
                }
            }
        }
        allDependentTextureGUIDs.Distinct();
    }

    private static void SetSubdirectories(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                textureFolderList.Add(subdirectory);
            }
        }
        else
        {
            Debug.LogError("目录不存在");
        }
    }
    private static void PackByDependent()
    {
        atlasName_Comm = config.atlasName_Comm;
        atlasName_Comm_big = config.atlasName_Comm_big;

        altasDic.Clear();
        altasDic.Add(atlasName_Comm, new List<Sprite>());
        altasDic.Add(atlasName_Comm_big, new List<Sprite>());

        repeatResDic.Clear();
        mergeDic.Clear();
        prefabPathList.Clear();

        EditorUtilityExtensions.CheckRes(config.checkAssetsPath, ".prefab", (_path) => { prefabPathList.Add(_path); });
        SetRepeatResDic();
        MergeTexResForPrefab();
        SetAltasDicByDependent();
        OnGenerateAtlas();

        repeatResDic.Clear();
        mergeDic.Clear();
        altasDic.Clear();
        prefabPathList.Clear();
    }

    private static void OnGenerateAtlas()
    {
        SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings();
        packingSettings.enableRotation = false;
        packingSettings.enableTightPacking = false;
        packingSettings.padding = 4;

        SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings();
        textureSettings.generateMipMaps = false;
        textureSettings.sRGB = true;
        textureSettings.filterMode = FilterMode.Bilinear;

        foreach (KeyValuePair<string, List<Sprite>> keyValuePair in altasDic)
        {
            string atlasName = keyValuePair.Key;
            List<Sprite> sprites = keyValuePair.Value;
            if (sprites.Count == 0) continue;

            string atlasPath = config.outputDirectory + "/" + atlasName + ".spriteatlas";
            SpriteAtlas atlas = new SpriteAtlas(); // 创建新的SpriteAtlas
            atlas.Add(sprites.ToArray()); // 将纹理数组添加到SpriteAtlas
            atlas.SetPackingSettings(packingSettings);
            atlas.SetTextureSettings(textureSettings);

            // 保存SpriteAtlas
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();

            Debug.Log("图集生成完成，保存在：" + atlasPath);
        }
        // 刷新资源
        AssetDatabase.Refresh();
    }
    private static void MergeTexResForPrefab()
    {
        foreach (KeyValuePair<string, HashSet<MergeInfo>> keyValuePair in mergeDic)
        {
            string shortPath = keyValuePair.Key;
            string FolderStrInfo = File.ReadAllText(shortPath);
            List<MergeInfo> merges = keyValuePair.Value.Distinct().ToList();
            foreach (MergeInfo mergeInfo in merges)
            {
                //开始合并
                string currGuid = mergeInfo.currGuid;
                string targetGuid = mergeInfo.targetGuid;
                FolderStrInfo = FolderStrInfo.Replace(currGuid, targetGuid);
            }

            //重置预制体依赖
            if (File.Exists(shortPath)) File.Delete(shortPath);
            using (FileStream Folder = File.Create(shortPath))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(FolderStrInfo);
                Folder.Write(bytes, 0, bytes.Length);
                Folder.Flush();
                Folder.Close();
            }
        }
    }

    private static void SetAltasDicByDependent()
    {
        List<string> atlasNames = altasDic.Keys.ToList();
        foreach (string atlasName in atlasNames)
        {
            foreach (KeyValuePair<string, TextureResInfo> keyValuePair in repeatResDic)
            {
                TextureResInfo info = keyValuePair.Value;
                if (info.altasName != atlasName) continue;

                Sprite sprite = info.sprite;
                altasDic[atlasName].Add(sprite);
            }
        }
    }
    private static void SetRepeatResDic()
    {
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            int index = i;
            string _path = prefabPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            UnityEngine.Object checkObj = AssetDatabase.LoadAssetAtPath(shortPath, typeof(UnityEngine.Object));

            SetRepeatResDic(checkObj, shortPath);
        }
    }
    private static string GetObjFolderName(string objPath)
    {
        FileInfo prefabFolderInfo = new FileInfo(objPath);
        if (prefabFolderInfo.Exists)
        {
            DirectoryInfo prefabDirectory = prefabFolderInfo.Directory;
            if (prefabDirectory != null)
            {
                return prefabDirectory.Name;
            }
            else
            {
                return "Invalid Directory";
            }
        }
        else
        {
            return "Folder not found";
        }
    }
    private static void SetRepeatResDic(UnityEngine.Object targetObject, string shortPath)
    {
        string atlasName = GetObjFolderName(shortPath);
        string atlasName_big = atlasName + "_big";
        string checkTypeName = typeof(Sprite).FullName;

        if (!altasDic.ContainsKey(atlasName)) altasDic.Add(atlasName, new List<Sprite>());
        if (!altasDic.ContainsKey(atlasName_big)) altasDic.Add(atlasName_big, new List<Sprite>());
        if (targetObject != null)
        {
            UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { targetObject });
            foreach (UnityEngine.Object dependency in dependencies)
            {
                if (!dependency.GetType().FullName.Contains(checkTypeName)) continue;
                string filePath = AssetDatabase.GetAssetPath(dependency);
                if (!File.Exists(filePath)) continue;
                string md5 = GetMD5(filePath);
                Sprite sprite = dependency as Sprite;
                bool isExist = repeatResDic.ContainsKey(md5);
                bool isBig = sprite.rect.width >= config.maxSize || sprite.rect.height >= config.maxSize;
                bool isCommon = isExist ? repeatResDic[md5].altasName != (isBig ? atlasName_big : atlasName) : false;
                if (isCommon)
                {
                    repeatResDic[md5].altasName = isBig ? atlasName_Comm_big : atlasName_Comm;

                    //处理合并信息
                    string currGuid = AssetDatabase.AssetPathToGUID(filePath);
                    string targetGuid = AssetDatabase.AssetPathToGUID(repeatResDic[md5].path);
                    CheckMerge(shortPath, currGuid, targetGuid);
                }
                else if (!isExist)
                {
                    repeatResDic.Add(md5, new TextureResInfo() { sprite = sprite, md5 = md5, path = filePath });
                    repeatResDic[md5].altasName = isBig ? atlasName_big : atlasName;
                }
            }
        }
        else
        {
            Debug.LogWarning("No game object selected.");
        }
    }
    private static void CheckMerge(string prefabPath, string currGuid, string targetGuid)
    {
        if (currGuid == targetGuid) return;
        if (!mergeDic.ContainsKey(prefabPath)) mergeDic.Add(prefabPath, new HashSet<MergeInfo>());
        mergeDic[prefabPath].Add(new MergeInfo() { currGuid = currGuid, targetGuid = targetGuid });
    }
    public static string GetMD5(string FolderPath)
    {
        using (FileStream Folder = File.OpenRead(FolderPath))
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = md5.ComputeHash(Folder);
            Folder.Close();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in bytes) stringBuilder.Append(b.ToString("x2"));

            return stringBuilder.ToString();
        }
    }
}
public class TextureResInfo
{
    public string altasName;
    public Sprite sprite;
    public string md5;
    public string path;
}
public class MergeInfo
{
    public string currGuid;
    public string targetGuid;
}
