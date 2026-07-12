using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在 HUD_Result 下搭建可微调的「战斗胜利」双层美术（底层 + 字体）。
/// 菜单：AR封妖 / 搭建可手动调整的胜利UI
/// </summary>
public static class BuildVictoryResultUI
{
    private const string SkinRootName = "VictorySkin_可调";
    private const string BottomName = "Bottom_可调";
    private const string TitleName = "TitleFont_可调";
    private const string DetailName = "Detail_可调";
    private const string MessagePanelName = "MessagePanel_可调";
    private const string ResourceFolder = "Assets/Resources/BattleVictorySkin";

    [MenuItem("AR封妖/搭建可手动调整的胜利UI")]
    public static void Build()
    {
        EnsureSpritesImported();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("胜利UI", "场景中找不到 Canvas。", "OK");
            return;
        }

        Transform resultTf = canvas.transform.Find("HUD_Result");
        GameObject resultGo;
        if (resultTf == null)
        {
            resultGo = new GameObject("HUD_Result", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            resultGo.layer = 5;
            resultGo.transform.SetParent(canvas.transform, false);
            var rt = resultGo.GetComponent<RectTransform>();
            StretchFull(rt);
            var dim = resultGo.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
        }
        else
        {
            resultGo = resultTf.gameObject;
        }

        // 确保压暗底
        var rootImg = resultGo.GetComponent<Image>();
        if (rootImg == null)
            rootImg = resultGo.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.72f);
        rootImg.raycastTarget = true;
        StretchFull(resultGo.GetComponent<RectTransform>());

        // 旧 Panel 重命名为 MessagePanel_可调（失败/通用提示）
        Transform oldPanel = resultGo.transform.Find("Panel");
        if (oldPanel != null && resultGo.transform.Find(MessagePanelName) == null)
            oldPanel.name = MessagePanelName;

        Transform msgTf = resultGo.transform.Find(MessagePanelName);
        if (msgTf == null)
        {
            var msgGo = CreateUiObject(MessagePanelName, resultGo.transform);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin = msgRt.anchorMax = new Vector2(0.5f, 0.5f);
            msgRt.pivot = new Vector2(0.5f, 0.5f);
            msgRt.anchoredPosition = Vector2.zero;
            msgRt.sizeDelta = new Vector2(460, 260);
            var msgBg = msgGo.AddComponent<Image>();
            msgBg.color = new Color(0.1f, 0.09f, 0.12f, 0.96f);
            msgTf = msgGo.transform;

            var textGo = CreateUiObject("ResultText", msgTf);
            StretchFull(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "提示";
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            TmpChineseFontUtil.BindChineseFont(tmp);
        }

        // —— 胜利皮肤根 ——
        Transform skinTf = resultGo.transform.Find(SkinRootName);
        if (skinTf != null)
            Object.DestroyImmediate(skinTf.gameObject);

        var skinGo = CreateUiObject(SkinRootName, resultGo.transform);
        var skinRt = skinGo.GetComponent<RectTransform>();
        StretchFull(skinRt);
        // 不拦截按钮点击（按钮在同级）
        var skinRay = skinGo.GetComponent<Image>();
        if (skinRay != null) Object.DestroyImmediate(skinRay);

        Sprite bottomSp = LoadSprite("victory_bottom");
        Sprite titleSp = LoadSprite("victory_title");
        if (bottomSp == null || titleSp == null)
        {
            EditorUtility.DisplayDialog("胜利UI",
                "找不到胜利美术 Sprite。\n请确认：\n" + ResourceFolder + "/victory_bottom.png\n" + ResourceFolder + "/victory_title.png\n\n并等待 Unity 导入完成后再跑本菜单。",
                "OK");
            return;
        }

        // 底层
        var bottomGo = CreateUiObject(BottomName, skinGo.transform);
        var bottomImg = bottomGo.AddComponent<Image>();
        bottomImg.sprite = bottomSp;
        bottomImg.preserveAspect = true;
        bottomImg.raycastTarget = false;
        bottomImg.color = Color.white;

        // 上层字体（sibling 在后 = 渲染在上）
        var titleGo = CreateUiObject(TitleName, skinGo.transform);
        var titleImg = titleGo.AddComponent<Image>();
        titleImg.sprite = titleSp;
        titleImg.preserveAspect = true;
        titleImg.raycastTarget = false;
        titleImg.color = Color.white;

        // 副标题
        var detailGo = CreateUiObject(DetailName, skinGo.transform);
        var detailTmp = detailGo.AddComponent<TextMeshProUGUI>();
        detailTmp.text = "请扫描第 2 关卡牌继续挑战";
        detailTmp.fontSize = 28;
        detailTmp.alignment = TextAlignmentOptions.Center;
        detailTmp.color = new Color(1f, 0.92f, 0.75f, 1f);
        detailTmp.enableWordWrapping = true;
        detailTmp.raycastTarget = false;
        TmpChineseFontUtil.BindChineseFont(detailTmp);

        var skin = skinGo.AddComponent<BattleResultVictorySkin>();
        skin.skinRoot = skinRt;
        skin.bottomLayer = bottomGo.GetComponent<RectTransform>();
        skin.bottomImage = bottomImg;
        skin.titleLayer = titleGo.GetComponent<RectTransform>();
        skin.titleImage = titleImg;
        skin.detailRoot = detailGo.GetComponent<RectTransform>();
        skin.detailText = detailTmp;
        skin.bottomSprite = bottomSp;
        skin.titleSprite = titleSp;
        skin.bottomSize = new Vector2(720f, 1020f);
        skin.titleSize = new Vector2(640f, 900f);
        skin.bottomAnchoredPos = new Vector2(0f, 40f);
        skin.titleAnchoredPos = new Vector2(0f, 50f);
        skin.detailSize = new Vector2(780f, 110f);
        skin.detailAnchoredPos = new Vector2(0f, -300f);
        skin.detailFontSize = 28f;
        skin.overallScale = 1f;
        skin.ApplyLayout();

        // 主按钮：尽量复用已有
        Button primaryBtn = null;
        TextMeshProUGUI primaryLabel = null;
        Transform btnTf = resultGo.transform.Find("Btn_Restart");
        if (btnTf == null) btnTf = resultGo.transform.Find("Btn_Primary_可调");
        if (btnTf != null)
        {
            // 提到 HUD_Result 根下，避免被皮肤挡住层级
            btnTf.SetParent(resultGo.transform, true);
            btnTf.name = "Btn_Primary_可调";
            primaryBtn = btnTf.GetComponent<Button>();
            primaryLabel = btnTf.GetComponentInChildren<TextMeshProUGUI>(true);
            var btnRt = btnTf.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.pivot = new Vector2(0.5f, 0.5f);
                btnRt.anchoredPosition = new Vector2(0f, -420f);
                btnRt.sizeDelta = new Vector2(240f, 64f);
            }
        }
        else
        {
            var btnGo = CreateUiObject("Btn_Primary_可调", resultGo.transform);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0f, -420f);
            btnRt.sizeDelta = new Vector2(240f, 64f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.12f, 0.1f, 0.95f);
            primaryBtn = btnGo.AddComponent<Button>();
            primaryBtn.targetGraphic = btnImg;
            var labelGo = CreateUiObject("Label", btnGo.transform);
            StretchFull(labelGo.GetComponent<RectTransform>());
            primaryLabel = labelGo.AddComponent<TextMeshProUGUI>();
            primaryLabel.text = "再来一局";
            primaryLabel.fontSize = 26;
            primaryLabel.alignment = TextAlignmentOptions.Center;
            primaryLabel.color = new Color(1f, 0.9f, 0.7f, 1f);
            TmpChineseFontUtil.BindChineseFont(primaryLabel);
        }

        // 绑定 BattleResultUI（挂在 Canvas 或 HUD_Result）
        var bru = Object.FindFirstObjectByType<BattleResultUI>();
        if (bru == null)
            bru = canvas.gameObject.AddComponent<BattleResultUI>();

        bru.panelRoot = resultGo;
        bru.victorySkin = skin;
        bru.messagePanel = msgTf != null ? msgTf.gameObject : null;

        TextMeshProUGUI resultTmp = null;
        if (msgTf != null)
            resultTmp = msgTf.GetComponentInChildren<TextMeshProUGUI>(true);
        if (resultTmp != null)
            bru.resultText = resultTmp;

        if (primaryBtn != null)
            bru.restartButton = primaryBtn;
        if (primaryLabel != null)
            bru.restartButtonLabel = primaryLabel;

        if (bru.turnManager == null)
            bru.turnManager = Object.FindFirstObjectByType<TurnManager>();

        // 默认隐藏
        skin.ShowSkin(false);
        if (msgTf != null) msgTf.gameObject.SetActive(false);
        if (primaryBtn != null) primaryBtn.gameObject.SetActive(false);
        resultGo.SetActive(false);

        // 层级：皮肤在消息框之上、按钮最上
        skinGo.transform.SetSiblingIndex(0);
        if (msgTf != null) msgTf.SetSiblingIndex(1);
        if (primaryBtn != null) primaryBtn.transform.SetAsLastSibling();

        EditorUtility.SetDirty(resultGo);
        EditorUtility.SetDirty(bru);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = skinGo;
        EditorUtility.DisplayDialog("胜利UI",
            "已搭建 VictorySkin_可调：\n" +
            "• Bottom_可调 — 底层墨圈\n" +
            "• TitleFont_可调 — 「战斗胜利」字体（在上层）\n" +
            "• Detail_可调 — 副标题\n" +
            "• Btn_Primary_可调 — 主按钮\n\n" +
            "可在 Hierarchy 直接拖 Rect，或选中 VictorySkin_可调 在 Inspector 调 overallScale / size / offset。\n" +
            "失败/AR 提示仍用 MessagePanel_可调。",
            "OK");
    }

    private static void EnsureSpritesImported()
    {
        string[] paths =
        {
            ResourceFolder + "/victory_bottom.png",
            ResourceFolder + "/victory_title.png"
        };
        foreach (var p in paths)
        {
            var importer = AssetImporter.GetAtPath(p) as TextureImporter;
            if (importer == null) continue;
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
    }

    private static Sprite LoadSprite(string name)
    {
        // Resources 路径
        var sp = Resources.Load<Sprite>("BattleVictorySkin/" + name);
        if (sp != null) return sp;

        string path = ResourceFolder + "/" + name + ".png";
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
