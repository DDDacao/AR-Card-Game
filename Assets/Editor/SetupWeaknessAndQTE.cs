using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// 一键：在敌人胸口挂红色弱点 + 创建 QTE UI + QTEManager
/// 菜单：AR封妖 / 配置弱点与QTE
/// </summary>
public static class SetupWeaknessAndQTE
{
    [MenuItem("AR封妖/配置弱点与QTE")]
    public static void Setup()
    {
        // ---- 1) 弱点挂到 Ellen ----
        GameObject enemy = GameObject.Find("Ellen_skin (2)");
        if (enemy == null)
        {
            // 尝试 ImageTarget 下的
            var it = GameObject.Find("ImageTarget");
            if (it != null)
            {
                var t = it.transform.Find("Ellen_skin (1)");
                if (t != null) enemy = t.gameObject;
            }
        }
        if (enemy == null)
        {
            EditorUtility.DisplayDialog("配置弱点与QTE", "找不到敌人模型 Ellen_skin", "OK");
            return;
        }

        // 清旧弱点
        var olds = enemy.GetComponentsInChildren<WeaknessPoint>(true);
        for (int i = 0; i < olds.Length; i++)
            Object.DestroyImmediate(olds[i].gameObject);

        var wpGo = new GameObject("Weakness_Red");
        wpGo.transform.SetParent(enemy.transform, false);
        // 胸口大致位置（相对 Ellen 本地）
        // 略靠前，避免完全埋在身体碰撞盒里
        wpGo.transform.localPosition = new Vector3(0f, 1.2f, 0.35f);
        wpGo.transform.localRotation = Quaternion.identity;
        wpGo.transform.localScale = Vector3.one;

        var sphere = wpGo.AddComponent<SphereCollider>();
        sphere.radius = 0.45f;
        sphere.isTrigger = false;

        var wp = wpGo.AddComponent<WeaknessPoint>();
        wp.weaknessType = WeaknessType.RedAttack;
        wp.markerScale = 0.5f;
        wp.owner = enemy.GetComponent<CharacterStats>();
        wp.showMarker = true;

        // 确保敌人本体有可被打到的 Collider（可选保留）
        if (enemy.GetComponent<Collider>() == null && enemy.GetComponentInChildren<Collider>() == null)
        {
            var box = enemy.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.9f, 0);
            box.size = new Vector3(0.8f, 1.8f, 0.5f);
        }

        // ---- 2) QTEManager ----
        var qteMgrGo = GameObject.Find("QTEManager");
        if (qteMgrGo == null) qteMgrGo = new GameObject("QTEManager");
        var qteMgr = qteMgrGo.GetComponent<QTEManager>();
        if (qteMgr == null) qteMgr = qteMgrGo.AddComponent<QTEManager>();
        qteMgr.defaultDuration = 2f;
        qteMgr.defaultRequiredClicks = 3;

        // ---- 3) QTE UI on Canvas ----
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("配置弱点与QTE", "找不到 Canvas", "OK");
            return;
        }

        // 删旧
        var oldQte = canvas.transform.Find("HUD_QTE");
        if (oldQte != null) Object.DestroyImmediate(oldQte.gameObject);

        TMP_FontAsset font = FindChineseFont();

        var qteRoot = CreatePanel("HUD_QTE", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
        FullStretch(qteRoot);

        var panel = CreatePanel("Panel", qteRoot.transform, new Color(0.08f, 0.07f, 0.1f, 0.95f));
        Stretch(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520, 340));
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.25f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2, -2);

        var title = CreateTmp("Title", panel.transform, font, 28, new Color(0.95f, 0.9f, 0.85f), TextAlignmentOptions.Center);
        Stretch(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -24), new Vector2(460, 40));
        title.GetComponent<TextMeshProUGUI>().text = "命中破绽！快速点击封印";

        var progress = CreateTmp("Progress", panel.transform, font, 48, Color.white, TextAlignmentOptions.Center);
        Stretch(progress, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200, 60));
        progress.GetComponent<TextMeshProUGUI>().text = "0 / 3";

        var timer = CreateTmp("Timer", panel.transform, font, 26, new Color(1f, 0.75f, 0.35f), TextAlignmentOptions.Center);
        Stretch(timer, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(160, 36));
        timer.GetComponent<TextMeshProUGUI>().text = "2.0s";

        // 时间条
        var sliderGo = new GameObject("TimeSlider", typeof(RectTransform), typeof(Slider));
        sliderGo.layer = 5;
        sliderGo.transform.SetParent(panel.transform, false);
        Stretch(sliderGo, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(360, 18));
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 2; slider.value = 2;
        slider.interactable = false;
        var sBg = CreatePanel("Background", sliderGo.transform, new Color(0.2f, 0.2f, 0.22f, 1f));
        FullStretch(sBg);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = 5;
        fillArea.transform.SetParent(sliderGo.transform, false);
        FullStretch(fillArea);
        var fill = CreatePanel("Fill", fillArea.transform, new Color(0.9f, 0.3f, 0.2f, 1f));
        FullStretch(fill);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill.GetComponent<Image>();

        // 大点击按钮
        var btnGo = CreatePanel("TapButton", panel.transform, new Color(0.75f, 0.2f, 0.18f, 1f));
        Stretch(btnGo, new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(280, 70));
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnGo.GetComponent<Image>();
        var btnLabel = CreateTmp("Label", btnGo.transform, font, 32, Color.white, TextAlignmentOptions.Center);
        FullStretch(btnLabel);
        btnLabel.GetComponent<TextMeshProUGUI>().text = "点  击";

        var result = CreateTmp("Result", panel.transform, font, 30, Color.white, TextAlignmentOptions.Center);
        Stretch(result, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400, 50));
        result.GetComponent<TextMeshProUGUI>().text = "";
        result.gameObject.SetActive(false);

        var panelUI = qteRoot.AddComponent<QTEPanelUI>();
        panelUI.root = qteRoot;
        panelUI.titleText = title.GetComponent<TextMeshProUGUI>();
        panelUI.progressText = progress.GetComponent<TextMeshProUGUI>();
        panelUI.timerText = timer.GetComponent<TextMeshProUGUI>();
        panelUI.resultText = result.GetComponent<TextMeshProUGUI>();
        panelUI.tapButton = btn;
        panelUI.timeSlider = slider;

        qteMgr.panelUI = panelUI;
        qteRoot.SetActive(false);

        // 确保敌人有 CharacterStats 被 TurnManager 找到
        var tm = Object.FindAnyObjectByType<TurnManager>();
        if (tm != null)
        {
            var stats = enemy.GetComponent<CharacterStats>();
            if (stats != null) tm.enemyStats = stats;
            EditorUtility.SetDirty(tm);
        }

        EditorUtility.SetDirty(enemy);
        EditorUtility.SetDirty(qteMgrGo);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupWeaknessAndQTE] 红色弱点 + QTE UI 配置完成。攻击牌指向胸口红点可触发 2 秒点 3 次。");
        EditorUtility.DisplayDialog("配置弱点与QTE",
            "已完成：\n• 敌人胸口红色弱点\n• QTE 面板（2秒点3次）\n• QTEManager\n\nPlay 后用攻击牌指向红点触发。",
            "OK");
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject CreateTmp(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.extraPadding = true;
        return go;
    }

    static void Stretch(GameObject go, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 anchored, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchored;
        rt.sizeDelta = size;
    }

    static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_FontAsset FindChineseFont()
    {
        string[] names = { "ziti", "2", "3" };
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa == null) continue;
            for (int n = 0; n < names.Length; n++)
                if (fa.name == names[n]) return fa;
        }
        return null;
    }
}
