using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 搭建对局设置按钮 + 面板。
/// 菜单：AR封妖 / 搭建设置界面（重开本关）
/// </summary>
public static class BuildSettingsUI
{
    const string IconPath = "Assets/Resources/BattleHudSkin/settings_icon.png";
    const string RootName = "HUD_Settings";

    [MenuItem("AR封妖/搭建设置界面（重开本关）")]
    public static void Build()
    {
        EnsureIconImport();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("设置界面", "场景中找不到 Canvas。", "OK");
            return;
        }

        // 清理旧节点
        Transform old = canvas.transform.Find(RootName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Sprite iconSp = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        if (iconSp == null)
            iconSp = Resources.Load<Sprite>("BattleHudSkin/settings_icon");
        if (iconSp == null)
        {
            EditorUtility.DisplayDialog("设置界面",
                "找不到 settings_icon。\n请确认：\n" + IconPath, "OK");
            return;
        }

        var root = CreateUi(RootName, canvas.transform);
        StretchFull(root.GetComponent<RectTransform>());
        // 根不挡点击
        var rootImg = root.GetComponent<Image>();
        if (rootImg != null) Object.DestroyImmediate(rootImg);

        // —— 设置按钮（右上角，可手调）——
        var btnGo = CreateUi("SettingsButton_可调", root.transform);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot = new Vector2(1f, 1f);
        btnRt.anchoredPosition = new Vector2(-28f, -28f);
        btnRt.sizeDelta = new Vector2(88f, 88f);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = iconSp;
        btnImg.preserveAspect = true;
        btnImg.color = Color.white;
        btnImg.raycastTarget = true;
        var settingsBtn = btnGo.AddComponent<Button>();
        settingsBtn.targetGraphic = btnImg;
        var colors = settingsBtn.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        settingsBtn.colors = colors;

        // —— 面板（全屏）——
        var panelGo = CreateUi("SettingsPanel", root.transform);
        StretchFull(panelGo.GetComponent<RectTransform>());
        panelGo.SetActive(false);

        // 半透明遮罩（点击关闭）
        var dimGo = CreateUi("Dim", panelGo.transform);
        StretchFull(dimGo.GetComponent<RectTransform>());
        var dimImg = dimGo.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.62f);
        dimImg.raycastTarget = true;
        var dimBtn = dimGo.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.transition = Selectable.Transition.None;

        // 中央内容框
        var boxGo = CreateUi("PanelBox_可调", panelGo.transform);
        var boxRt = boxGo.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(520f, 320f);
        var boxImg = boxGo.AddComponent<Image>();
        boxImg.color = new Color(0.08f, 0.07f, 0.1f, 0.94f);
        boxImg.raycastTarget = true;

        // 标题
        var titleGo = CreateUi("Title", boxGo.transform);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        titleRt.sizeDelta = new Vector2(-40f, 56f);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "设置";
        titleTmp.fontSize = 36;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.92f, 0.78f, 1f);
        TmpChineseFontUtil.BindChineseFont(titleTmp);

        // 重新开始本关
        var restartGo = CreateUi("Btn_RestartStage_可调", boxGo.transform);
        var restartRt = restartGo.GetComponent<RectTransform>();
        restartRt.anchorMin = restartRt.anchorMax = new Vector2(0.5f, 0.5f);
        restartRt.pivot = new Vector2(0.5f, 0.5f);
        restartRt.anchoredPosition = new Vector2(0f, 10f);
        restartRt.sizeDelta = new Vector2(360f, 72f);
        var restartImg = restartGo.AddComponent<Image>();
        restartImg.color = new Color(0.45f, 0.14f, 0.12f, 0.95f);
        var restartBtn = restartGo.AddComponent<Button>();
        restartBtn.targetGraphic = restartImg;

        var restartLabelGo = CreateUi("Label", restartGo.transform);
        StretchFull(restartLabelGo.GetComponent<RectTransform>());
        var restartLabel = restartLabelGo.AddComponent<TextMeshProUGUI>();
        restartLabel.text = "重新开始本关";
        restartLabel.fontSize = 30;
        restartLabel.alignment = TextAlignmentOptions.Center;
        restartLabel.color = new Color(1f, 0.9f, 0.75f, 1f);
        TmpChineseFontUtil.BindChineseFont(restartLabel);

        // 关闭
        var closeGo = CreateUi("Btn_Close_可调", boxGo.transform);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0f, 36f);
        closeRt.sizeDelta = new Vector2(200f, 56f);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.18f, 0.16f, 0.2f, 0.95f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;

        var closeLabelGo = CreateUi("Label", closeGo.transform);
        StretchFull(closeLabelGo.GetComponent<RectTransform>());
        var closeLabel = closeLabelGo.AddComponent<TextMeshProUGUI>();
        closeLabel.text = "关闭";
        closeLabel.fontSize = 26;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = new Color(0.9f, 0.88f, 0.84f, 1f);
        TmpChineseFontUtil.BindChineseFont(closeLabel);

        // 控制器
        var ui = root.AddComponent<BattleSettingsUI>();
        ui.settingsButton = settingsBtn;
        ui.settingsButtonRoot = btnGo;
        ui.panelRoot = panelGo;
        ui.restartStageButton = restartBtn;
        ui.closeButton = closeBtn;
        ui.dimBackgroundButton = dimBtn;
        ui.titleText = titleTmp;
        ui.restartLabel = restartLabel;
        ui.battleFlow = Object.FindFirstObjectByType<BattleFlowManager>();
        ui.turnManager = Object.FindFirstObjectByType<TurnManager>();
        ui.resultUI = Object.FindFirstObjectByType<BattleResultUI>();
        ui.hideButtonWhenNotInBattle = true;
        ui.blockCardsWhenOpen = true;

        // 层级：尽量靠前，但在 QTE/Result 之下也可；运行时 Open 会置顶
        root.transform.SetAsLastSibling();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = btnGo;
        EditorUtility.DisplayDialog("设置界面",
            "已搭建 HUD_Settings：\n" +
            "• SettingsButton_可调 — 右上角设置图标（可拖）\n" +
            "• SettingsPanel — 遮罩 + 面板\n" +
            "• Btn_RestartStage_可调 — 重新开始本关\n" +
            "• Btn_Close_可调 — 关闭\n\n" +
            "开战中显示设置按钮；点击后弹出面板，可重开本关。",
            "OK");
    }

    static void EnsureIconImport()
    {
        var imp = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (imp == null) return;
        bool dirty = false;
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            dirty = true;
        }
        if (!imp.alphaIsTransparency)
        {
            imp.alphaIsTransparency = true;
            dirty = true;
        }
        if (imp.mipmapEnabled)
        {
            imp.mipmapEnabled = false;
            dirty = true;
        }
        if (dirty) imp.SaveAndReimport();
        else AssetDatabase.ImportAsset(IconPath);
    }

    static GameObject CreateUi(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
