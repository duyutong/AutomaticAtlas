using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using UnityEngine.U2D;
using Unity.VisualScripting;
using System.Drawing;

public class ClearAtlas : EditorWindow
{
    private static string configPath = "Assets/Plugins/AutomaticAtlas/Editor/AtlasConfig.asset";
    private static AtlasConfig config;

    private Toggle togSelectAll;
    private ScrollView scrollView;
    private Button btnClearSelect;

    private List<AltasInfo> selectAltas = new List<AltasInfo>();
    private List<AltasInfo> spriteAtlas = new List<AltasInfo>();
    private List<AltasVisualElement> altasVisuals = new List<AltasVisualElement>();

    [MenuItem("Tools/ClearAtlas(ɾ��ͼ��)")]
    public static void ShowExample()
    {
        ClearAtlas wnd = GetWindow<ClearAtlas>();
        wnd.titleContent = new GUIContent("ClearAtlas");
        wnd.maxSize = new Vector2(480, 2000);
        wnd.minSize = new Vector2(480,300);
    }
    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        if (config == null) config = AssetDatabase.LoadAssetAtPath<AtlasConfig>(configPath);
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Plugins/AutomaticAtlas/Editor/UIBuilder/ClearAtlas.uxml");
        visualTree.CloneTree(root);

        Label altasPath = root.Q<Label>("altasPath");
        altasPath.text = config.outputDirectory;

        togSelectAll = root.Q<Toggle>("togSelectAll");
        togSelectAll.RegisterValueChangedCallback(OnTogSelectAllValueChanged);

        btnClearSelect = root.Q<Button>("btnClearSelect");
        btnClearSelect.clicked += OnBtnClearSelectClick;

        scrollView = root.Q<ScrollView>("scrollView");
        scrollView.contentContainer.Clear();

        SetAltasView();
        InitSelectAll();
    }

    private void InitSelectAll()
    {
        togSelectAll.SetValueWithoutNotify(true);
        foreach (AltasVisualElement altasVisual in altasVisuals)
        {
            altasVisual.SetSelectToggleValue(true);
        }
    }

    private void OnDisable()
    {
        btnClearSelect.clicked -= OnBtnClearSelectClick;
        togSelectAll.UnregisterValueChangedCallback(OnTogSelectAllValueChanged);

    }
    private void OnBtnClearSelectClick()
    {
        foreach (AltasInfo atlasInfo in selectAltas) 
        {
            atlasInfo.visualElement.Clear();
            AssetDatabase.DeleteAsset(atlasInfo.shortPath);
        }
    }

    private void OnTogSelectAllValueChanged(ChangeEvent<bool> evt)
    {
        bool isOn = evt.newValue == true;
        foreach (AltasVisualElement altasVisual in altasVisuals)
        {
            altasVisual.SetSelectToggleValue(isOn);
        }
    }
    private void OnClearSingleAltas(AltasInfo atlasInfo) 
    {
        spriteAtlas.Remove(atlasInfo);
        selectAltas.Remove(atlasInfo);
        AssetDatabase.DeleteAsset(atlasInfo.shortPath);
    }
    private void SetAltasView()
    {
        altasVisuals.Clear();
        spriteAtlas.Clear();
        selectAltas.Clear();
        EditorUtilityExtensions.CheckRes(config.outputDirectory, ".spriteatlas", (path) =>
        {
            string shortPath = EditorUtilityExtensions.ToShortPath(path);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(shortPath);
            AltasInfo altasInfo = new AltasInfo();
            altasInfo.shortPath = shortPath;
            altasInfo.atlas = atlas;
            spriteAtlas.Add(altasInfo);
        });
        foreach (AltasInfo atlasInfo in spriteAtlas)
        {
            AltasVisualElement element = new AltasVisualElement();
            element.SetAltasVE(atlasInfo);
            element.onClearAltas = OnClearSingleAltas;
            element.onSelectAltas = OnSelectSingleAltas;
            scrollView.Add(element);
            altasVisuals.Add(element);
        }
    }

    private void OnSelectSingleAltas(bool isOn, AltasInfo atlasInfo)
    {
        if (isOn) 
        {
            selectAltas.Add(atlasInfo); 
        }
        else
        {
            togSelectAll.SetValueWithoutNotify(false);
            selectAltas.Remove(atlasInfo);
        }
    }
}
public class AltasInfo 
{
    public SpriteAtlas atlas;
    public string shortPath;
    public AltasVisualElement visualElement;
}