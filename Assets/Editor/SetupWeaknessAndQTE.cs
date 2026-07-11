using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// 一键：弱点 + 镇魂铃 QTE UI + QTEManager
/// 菜单：AR封妖 / 配置弱点与QTE
///       AR封妖 / 重建镇魂铃QTE界面（不改弱点）
/// </summary>
public static class SetupWeaknessAndQTE
{
    const string BellSpritePath = "Assets/_ARSealCardGame/Art/UI/QTE/bell.png";
    const string RippleSpritePath = "Assets/_ARSealCardGame/Art/UI/QTE/ripple_ring.png";

    [MenuItem("AR封妖/配置弱点与QTE")]
    public static void Setup()
    {
        if (!SetupWeaknessOnly()) return;
        SetupQteUiOnly(showDialog: true);
    }

    [MenuItem("AR封妖/重建镇魂铃QTE界面")]
    public static void SetupQteOnlyMenu()
    {
        SetupQteUiOnly(showDialog: true);
    }

    /// <summary>仅重建 HUD_QTE + QTEManager 绑定，不碰弱点。</summary>
    public static void SetupQteUiOnly(bool showDialog)
    {
        EnsureBellImportSettings();
        EnsureRippleSprite();

        var qteMgrGo = GameObject.Find("QTEManager");
        if (qteMgrGo == null) qteMgrGo = new GameObject("QTEManager");
        var qteMgr = qteMgrGo.GetComponent<QTEManager>();
        if (qteMgr == null) qteMgr = qteMgrGo.AddComponent<QTEManager>();
        qteMgr.defaultDuration = 2f;
        qteMgr.defaultRequiredClicks = 3;

        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("镇魂铃QTE", "找不到 Canvas", "OK");
            return;
        }

        var oldQte = canvas.transform.Find("HUD_QTE");
        if (oldQte != null) Object.DestroyImmediate(oldQte.gameObject);

        TMP_FontAsset font = FindChineseFont();
        Sprite bellSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BellSpritePath);
        Sprite rippleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RippleSpritePath);

        // 根节点：全屏压暗 + 居中大铃铛 + 文字提示（压迫感版本）
        var qteRoot = new GameObject("HUD_QTE", typeof(RectTransform));
        qteRoot.layer = 5;
        qteRoot.transform.SetParent(canvas.transform, false);
        FullStretch(qteRoot);

        var dim = CreatePanel("Dim", qteRoot.transform, new Color(0f, 0f, 0f, 0.78f));
        FullStretch(dim);
        dim.GetComponent<Image>().raycastTarget = true;

        // 标题：铃铛正上方
        var title = CreateTmp("Title", qteRoot.transform, font, 32, new Color(0.98f, 0.92f, 0.78f), TextAlignmentOptions.Center);
        Stretch(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 210f), new Vector2(640, 44));
        title.GetComponent<TextMeshProUGUI>().text = "命中破绽！连敲镇魂铃";
        var titleOutline = title.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 进度
        var progress = CreateTmp("Progress", qteRoot.transform, font, 42, new Color(1f, 0.9f, 0.55f), TextAlignmentOptions.Center);
        Stretch(progress, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 165f), new Vector2(220, 48));
        progress.GetComponent<TextMeshProUGUI>().text = "0 / 3";
        var progressOutline = progress.AddComponent<Outline>();
        progressOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        progressOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 波纹 + 铃铛：屏幕正中，大热区
        var rippleRootGo = new GameObject("RippleRoot", typeof(RectTransform));
        rippleRootGo.layer = 5;
        rippleRootGo.transform.SetParent(qteRoot.transform, false);
        Stretch(rippleRootGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(280, 280));

        var bellRootGo = new GameObject("BellRoot", typeof(RectTransform));
        bellRootGo.layer = 5;
        bellRootGo.transform.SetParent(qteRoot.transform, false);
        Stretch(bellRootGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(280, 280));

        var bellBtnGo = new GameObject("BellButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        bellBtnGo.layer = 5;
        bellBtnGo.transform.SetParent(bellRootGo.transform, false);
        FullStretch(bellBtnGo);
        var bellImg = bellBtnGo.GetComponent<Image>();
        bellImg.color = Color.white;
        bellImg.preserveAspect = true;
        bellImg.raycastTarget = true;
        if (bellSprite != null)
        {
            bellImg.sprite = bellSprite;
            bellImg.type = Image.Type.Simple;
        }
        else
        {
            bellImg.color = new Color(0.85f, 0.7f, 0.25f, 1f);
            Debug.LogWarning("[SetupQTE] 未找到铃铛图: " + BellSpritePath);
        }

        var btn = bellBtnGo.GetComponent<Button>();
        btn.targetGraphic = bellImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.88f, 1f);
        colors.pressedColor = new Color(0.95f, 0.9f, 0.78f, 1f);
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        btn.colors = colors;

        // 提示
        var hint = CreateTmp("Hint", qteRoot.transform, font, 22, new Color(0.92f, 0.88f, 0.78f, 0.95f), TextAlignmentOptions.Center);
        Stretch(hint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -175f), new Vector2(420, 32));
        hint.GetComponent<TextMeshProUGUI>().text = "快速点按铃铛 3 次";
        var hintOutline = hint.AddComponent<Outline>();
        hintOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        hintOutline.effectDistance = new Vector2(1.2f, -1.2f);

        // 倒计时文字
        var timer = CreateTmp("Timer", qteRoot.transform, font, 28, new Color(1f, 0.78f, 0.4f), TextAlignmentOptions.Center);
        Stretch(timer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -215f), new Vector2(180, 36));
        timer.GetComponent<TextMeshProUGUI>().text = "2.0s";
        var timerOutline = timer.AddComponent<Outline>();
        timerOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        timerOutline.effectDistance = new Vector2(1.2f, -1.2f);

        // 细时间条
        var sliderGo = new GameObject("TimeSlider", typeof(RectTransform), typeof(Slider));
        sliderGo.layer = 5;
        sliderGo.transform.SetParent(qteRoot.transform, false);
        Stretch(sliderGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -250f), new Vector2(320, 10));
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 2; slider.value = 2;
        slider.interactable = false;
        var sBg = CreatePanel("Background", sliderGo.transform, new Color(0f, 0f, 0f, 0.45f));
        FullStretch(sBg);
        sBg.GetComponent<Image>().raycastTarget = false;
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = 5;
        fillArea.transform.SetParent(sliderGo.transform, false);
        FullStretch(fillArea);
        var fill = CreatePanel("Fill", fillArea.transform, new Color(0.95f, 0.72f, 0.25f, 1f));
        FullStretch(fill);
        fill.GetComponent<Image>().raycastTarget = false;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill.GetComponent<Image>();

        // 结果文案
        var result = CreateTmp("Result", qteRoot.transform, font, 36, Color.white, TextAlignmentOptions.Center);
        Stretch(result, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(480, 50));
        result.GetComponent<TextMeshProUGUI>().text = "";
        result.gameObject.SetActive(false);
        var resultOutline = result.AddComponent<Outline>();
        resultOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        resultOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var panelUI = qteRoot.AddComponent<QTEPanelUI>();
        panelUI.root = qteRoot;
        panelUI.titleText = title.GetComponent<TextMeshProUGUI>();
        panelUI.progressText = progress.GetComponent<TextMeshProUGUI>();
        panelUI.timerText = timer.GetComponent<TextMeshProUGUI>();
        panelUI.resultText = result.GetComponent<TextMeshProUGUI>();
        panelUI.tapButton = btn;
        panelUI.timeSlider = slider;
        panelUI.bellRoot = bellRootGo.GetComponent<RectTransform>();
        panelUI.bellImage = bellImg;
        panelUI.rippleRoot = rippleRootGo.GetComponent<RectTransform>();
        panelUI.rippleSprite = rippleSprite;
        panelUI.rippleColor = new Color(1f, 0.88f, 0.4f, 0.9f);

        qteMgr.panelUI = panelUI;
        qteRoot.transform.SetAsLastSibling();
        qteRoot.SetActive(false);

        EditorUtility.SetDirty(qteMgrGo);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupQTE] 镇魂铃 QTE：全屏压暗 + 居中大铃铛 + 文字提示。");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("镇魂铃QTE",
                "已恢复：\n• 居中大铃铛\n• 全屏压暗\n• 标题/进度/提示/倒计时\n\nPlay 后命中弱点触发。",
                "OK");
        }
    }

    static bool SetupWeaknessOnly()
    {
        GameObject enemy = GameObject.Find("Ellen_skin (2)");
        if (enemy == null)
        {
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
            return false;
        }

        var olds = enemy.GetComponentsInChildren<WeaknessPoint>(true);
        for (int i = 0; i < olds.Length; i++)
            Object.DestroyImmediate(olds[i].gameObject);

        var wpGo = new GameObject("Weakness_Red");
        wpGo.transform.SetParent(enemy.transform, false);
        wpGo.transform.localPosition = new Vector3(0f, 1.2f, 0.35f);
        wpGo.transform.localRotation = Quaternion.identity;
        wpGo.transform.localScale = Vector3.one;

        var sphere = wpGo.AddComponent<SphereCollider>();
        sphere.radius = 0.45f;
        sphere.isTrigger = false;

        var wp = wpGo.AddComponent<WeaknessPoint>();
        wp.weaknessType = WeaknessType.RedAttack;
        wp.visualCoreScale = 0.1f;
        wp.hitRadius = 0.62f;
        wp.owner = enemy.GetComponent<CharacterStats>();
        wp.showMarker = true;

        if (enemy.GetComponent<Collider>() == null && enemy.GetComponentInChildren<Collider>() == null)
        {
            var box = enemy.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.9f, 0);
            box.size = new Vector3(0.8f, 1.8f, 0.5f);
        }

        var tm = Object.FindAnyObjectByType<TurnManager>();
        if (tm != null)
        {
            var stats = enemy.GetComponent<CharacterStats>();
            if (stats != null) tm.enemyStats = stats;
            EditorUtility.SetDirty(tm);
        }

        EditorUtility.SetDirty(enemy);
        return true;
    }

    static void EnsureBellImportSettings()
    {
        var importer = AssetImporter.GetAtPath(BellSpritePath) as TextureImporter;
        if (importer == null) return;
        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }
        if (importer.alphaIsTransparency != true)
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

    static void EnsureRippleSprite()
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(RippleSpritePath) != null)
            return;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) * 0.5f;
        float outer = size * 0.48f;
        float inner = size * 0.34f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 0f;
                if (d <= outer && d >= inner)
                {
                    float edge = Mathf.Min(outer - d, d - inner);
                    a = Mathf.Clamp01(edge / 3f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        if (!AssetDatabase.IsValidFolder("Assets/_ARSealCardGame/Art"))
            AssetDatabase.CreateFolder("Assets/_ARSealCardGame", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/_ARSealCardGame/Art/UI"))
            AssetDatabase.CreateFolder("Assets/_ARSealCardGame/Art", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/_ARSealCardGame/Art/UI/QTE"))
            AssetDatabase.CreateFolder("Assets/_ARSealCardGame/Art/UI", "QTE");

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        string abs = System.IO.Path.Combine(Application.dataPath, "_ARSealCardGame/Art/UI/QTE/ripple_ring.png");
        System.IO.File.WriteAllBytes(abs, png);
        AssetDatabase.ImportAsset(RippleSpritePath);

        var importer = AssetImporter.GetAtPath(RippleSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
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
        // 优先 2/3（玩法汉字覆盖全）；ziti 缺字较多
        string[] names = { "2", "3", "ziti" };
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        TMP_FontAsset[] found = new TMP_FontAsset[names.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa == null) continue;
            for (int n = 0; n < names.Length; n++)
                if (fa.name == names[n]) found[n] = fa;
        }
        for (int n = 0; n < found.Length; n++)
            if (found[n] != null) return found[n];
        return null;
    }
}
