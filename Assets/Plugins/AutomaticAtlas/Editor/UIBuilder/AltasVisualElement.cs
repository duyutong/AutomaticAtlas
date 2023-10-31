using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class AltasVisualElement : VisualElement
{
    private Toggle toggle;
    private AltasInfo altasInfo;
    public Action<AltasInfo> onClearAltas;
    public Action<bool, AltasInfo> onSelectAltas;
    public new class UxmlFactory : UxmlFactory<AltasVisualElement, UxmlTraits> { }
    public void SetAltasVE(AltasInfo _atlasInfo)
    {
        altasInfo = _atlasInfo;
        altasInfo.visualElement = this;
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Plugins/AutomaticAtlas/Editor/UIBuilder/ClearAtlas.uss");
        styleSheets.Add(styleSheet);

        VisualElement visualElement = new VisualElement();
        visualElement.AddToClassList("AltasVisualElement");
        visualElement.style.flexDirection = FlexDirection.Row;
        contentContainer.Add(visualElement);

        ObjectField objectField = new ObjectField();
        objectField.objectType = typeof(SpriteAtlas);
        objectField.value = altasInfo.atlas;
        objectField.AddToClassList("objectField");
        visualElement.contentContainer.Add(objectField);

        Toolbar toolbar = new Toolbar();
        toolbar.AddToClassList("toolbar");
        visualElement.contentContainer.Add(toolbar);

        toggle = new Toggle();
        toggle.label = "Select";
        toggle.RegisterValueChangedCallback(OnToggleValueChange);
        toolbar.contentContainer.Add(toggle);

        Button btnClear = new Button();
        btnClear.AddToClassList("btnClear");
        btnClear.text = "Clear";
        btnClear.clicked += OnBtnClearClick;
        toolbar.contentContainer.Add(btnClear);
    }

    private void OnBtnClearClick()
    {
        style.display = DisplayStyle.None;
        onClearAltas?.Invoke(altasInfo);
    }

    private void OnToggleValueChange(ChangeEvent<bool> evt)
    {
        bool isOn = evt.newValue;
        onSelectAltas?.Invoke(isOn, altasInfo);
    }
    public void OnDeleteAltas() 
    {
        style.display = DisplayStyle.None;
    }
    public void SetSelectToggleValue(bool isOn)
    {
        toggle.SetValueWithoutNotify(isOn);
        onSelectAltas?.Invoke(isOn, altasInfo);
    }
}
