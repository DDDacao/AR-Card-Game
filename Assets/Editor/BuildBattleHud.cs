using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// 按《UI设计参考图》重建横屏战斗 HUD（1920x1080）。
/// 菜单：AR封妖 / 重建战斗HUD（横屏）
/// </summary>
public static class BuildBattleHud
{
    // 颜色
    static readonly Color PanelBg = new Color(0.06f, 0.05f, 0.08f, 0.88f);
    static readonly Color PanelBorder = new Color(0.55f, 0.42f, 0.22f, 0.75f);
    static readonly Color TextMain = new Color(0.95f, 0.93f, 0.88f, 1f);
    static readonly Color TextMuted = new Color(0.72f, 0.68f, 0.62f, 1f);
    static readonly Color HpRed = new Color(0.78f, 0.12f, 0.12f, 1f);
    static readonly Color HpBg = new Color(0.14f, 0.12f, 0.14f, 1f);
    static readonly Color AccentGold = new Color(0.72f, 0.52f, 0.22f, 1f);
    static readonly Color BtnBg = new Color(0.18f, 0.12f, 0.08f, 0.95f);
    static readonly Color IntentBg = new Color(0.12f, 0.10f, 0.22f, 0.92f);

    [MenuItem("AR封妖/重建战斗HUD（横屏）")]
    public static void Build()
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            EditorUtility.DisplayDialog("重建HUD", "场景中找不到 Canvas", "OK");
            return;
        }

        // 清掉旧 UI 子物体
        string[] toRemove =
        {
            "PlayerHealthBar", "EllenHealthBar", "EndPlayerTurn",
            "HUD_Player", "HUD_Info", "HUD_EnemyInfo", "HUD_Enemy", "HUD_SideInfo",
            "HUD_Actions", "HUD_Result", "HUD_Root"
        };
        for (int i = canvasGo.transform.childCount - 1; i >= 0; i--)
        {
            var child = canvasGo.transform.GetChild(i);
            for (int k = 0; k < toRemove.Length; k++)
            {
                if (child.name == toRemove[k] || child.name.StartsWith("HUD_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                    break;
                }
            }
        }

        // 去掉 Canvas 上旧的 BattleResultUI
        var oldResult = canvasGo.GetComponent<BattleResultUI>();
        if (oldResult != null) Object.DestroyImmediate(oldResult);
        var oldInfo = canvasGo.GetComponent<BattleInfoUI>();
        if (oldInfo != null) Object.DestroyImmediate(oldInfo);
        var oldPlayer = canvasGo.GetComponent<PlayerStatusUI>();
        if (oldPlayer != null) Object.DestroyImmediate(oldPlayer);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // 横屏手机平衡

        if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            canvasGo.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = FindChineseFont();

        // ========== ① 敌人信息 顶中（名 → 血条 → 意图） ==========
        var enemyRoot = CreatePanel("HUD_Enemy", canvasGo.transform, PanelBg);
        Stretch(enemyRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -16), new Vector2(520, 118));

        // 名称居中顶部
        var enemyName = CreateTmp("EnemyName", enemyRoot.transform, font, 26, TextMain, TextAlignmentOptions.Center);
        Stretch(enemyName, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -8), new Vector2(-16, 30));
        ConfigureTmp(enemyName.GetComponent<TextMeshProUGUI>(), "小妖", 26, false);

        // 血条在名称下方
        var enemyHp = CreateHpBar("EnemyHP", enemyRoot.transform, font, 460, 20);
        Stretch(enemyHp.root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -46), new Vector2(480, 28));

        // 意图在血条下方（用户要求）
        var intentBadge = CreatePanel("IntentBadge", enemyRoot.transform, IntentBg);
        Stretch(intentBadge, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 10), new Vector2(200, 30));
        var intentText = CreateTmp("IntentText", intentBadge.transform, font, 20, TextMain, TextAlignmentOptions.Center);
        FullStretch(intentText);
        ConfigureTmp(intentText.GetComponent<TextMeshProUGUI>(), "普通攻击", 20, false);

        var enemyHb = enemyRoot.AddComponent<HealthBarUI>();
        enemyHb.isPlayer = false;
        enemyHb.hpSlider = enemyHp.slider;
        enemyHb.hpText = enemyHp.hpText;
        enemyHb.hpFill = enemyHp.fill;

        // ========== ③ 玩家状态 右上 ==========
        var playerRoot = CreatePanel("HUD_Player", canvasGo.transform, PanelBg);
        Stretch(playerRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20, -20), new Vector2(300, 168));

        var pTitle = CreateTmp("Title", playerRoot.transform, font, 22, TextMuted, TextAlignmentOptions.Left);
        Stretch(pTitle, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10), new Vector2(-24, 28));
        pTitle.GetComponent<TextMeshProUGUI>().text = "  玩家状态";
        pTitle.GetComponent<TextMeshProUGUI>().margin = new Vector4(8, 0, 0, 0);

        var pHpLabel = CreateTmp("HPLabel", playerRoot.transform, font, 18, TextMuted, TextAlignmentOptions.Left);
        Stretch(pHpLabel, new Vector2(0, 1), new Vector2(0.35f, 1), new Vector2(0, 1),
            new Vector2(14, -44), new Vector2(90, 24));
        pHpLabel.GetComponent<TextMeshProUGUI>().text = "生命";

        var pHpBar = CreateHpBar("PlayerHP", playerRoot.transform, font, 200, 16);
        Stretch(pHpBar.root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -68), new Vector2(-28, 28));

        // 护甲行
        var armorRow = CreateEmpty("ArmorRow", playerRoot.transform);
        Stretch(armorRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -104), new Vector2(-28, 28));
        var armorLabel = CreateTmp("ArmorLabel", armorRow.transform, font, 18, TextMuted, TextAlignmentOptions.Left);
        Stretch(armorLabel, new Vector2(0, 0.5f), new Vector2(0.4f, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 0), new Vector2(80, 24));
        armorLabel.GetComponent<TextMeshProUGUI>().text = "护甲";
        var armorVal = CreateTmp("ArmorValue", armorRow.transform, font, 22, new Color(0.92f, 0.78f, 0.28f), TextAlignmentOptions.Left);
        Stretch(armorVal, new Vector2(0.4f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 0), new Vector2(100, 28));
        armorVal.GetComponent<TextMeshProUGUI>().text = "0";

        // 灵气行
        var energyRow = CreateEmpty("EnergyRow", playerRoot.transform);
        Stretch(energyRow, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            new Vector2(0, 14), new Vector2(-28, 28));
        var energyLabel = CreateTmp("EnergyLabel", energyRow.transform, font, 18, TextMuted, TextAlignmentOptions.Left);
        Stretch(energyLabel, new Vector2(0, 0.5f), new Vector2(0.4f, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 0), new Vector2(80, 24));
        energyLabel.GetComponent<TextMeshProUGUI>().text = "灵气";
        var energyVal = CreateTmp("EnergyValue", energyRow.transform, font, 22, new Color(0.35f, 0.75f, 0.95f), TextAlignmentOptions.Left);
        Stretch(energyVal, new Vector2(0.4f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 0), new Vector2(100, 28));
        energyVal.GetComponent<TextMeshProUGUI>().text = "3/3";

        var psu = playerRoot.AddComponent<PlayerStatusUI>();
        psu.autoFindPlayer = true;
        psu.hpSlider = pHpBar.slider;
        psu.hpText = pHpBar.hpText;
        psu.hpFill = pHpBar.fill;
        psu.armorContainer = armorRow;
        psu.armorText = armorVal.GetComponent<TextMeshProUGUI>();
        psu.armorLabel = armorLabel.GetComponent<TextMeshProUGUI>();
        psu.energyText = energyVal.GetComponent<TextMeshProUGUI>();
        psu.energyLabel = energyLabel.GetComponent<TextMeshProUGUI>();

        // ========== ④ 提示区 右中（分行固定槽位，避免黏连） ==========
        var sideRoot = CreatePanel("HUD_SideInfo", canvasGo.transform, PanelBg);
        Stretch(sideRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-20, 30), new Vector2(280, 200));

        // 行 1：回合
        var turnT = CreateTmp("TurnText", sideRoot.transform, font, 24, TextMain, TextAlignmentOptions.Left);
        Stretch(turnT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -12), new Vector2(-28, 32));
        ConfigureTmp(turnT.GetComponent<TextMeshProUGUI>(), "第 1 回合", 24, false);
        turnT.GetComponent<TextMeshProUGUI>().margin = new Vector4(14, 2, 14, 2);

        // 分隔线
        var sep1 = CreatePanel("Sep1", sideRoot.transform, new Color(1f, 1f, 1f, 0.12f));
        Stretch(sep1, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -48), new Vector2(0, 2));
        sep1.GetComponent<Image>().raycastTarget = false;

        // 行 2：牌堆
        var deckT = CreateTmp("DeckText", sideRoot.transform, font, 20, TextMuted, TextAlignmentOptions.Left);
        Stretch(deckT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -58), new Vector2(-28, 30));
        ConfigureTmp(deckT.GetComponent<TextMeshProUGUI>(), "牌堆剩余：5", 20, false);
        deckT.GetComponent<TextMeshProUGUI>().margin = new Vector4(14, 2, 14, 2);

        // 分隔线
        var sep2 = CreatePanel("Sep2", sideRoot.transform, new Color(1f, 1f, 1f, 0.12f));
        Stretch(sep2, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -92), new Vector2(0, 2));
        sep2.GetComponent<Image>().raycastTarget = false;

        // 行 3：提示（两行，行距加大）
        var hintT = CreateTmp("HintText", sideRoot.transform, font, 18, TextMuted, TextAlignmentOptions.TopLeft);
        Stretch(hintT, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0),
            new Vector2(0, 12), new Vector2(-28, -108));
        var hintTmp = hintT.GetComponent<TextMeshProUGUI>();
        ConfigureTmp(hintTmp, "拖出符咒攻击或防御\n点击结束回合", 18, true);
        hintTmp.margin = new Vector4(14, 4, 14, 8);
        hintTmp.lineSpacing = 12f;

        var biu = sideRoot.AddComponent<BattleInfoUI>();
        biu.turnText = turnT.GetComponent<TextMeshProUGUI>();
        biu.deckCountText = deckT.GetComponent<TextMeshProUGUI>();
        biu.hintText = hintTmp;
        biu.enemyNameText = enemyName.GetComponent<TextMeshProUGUI>();
        biu.enemyIntentText = intentText.GetComponent<TextMeshProUGUI>();
        biu.enemyDisplayName = "小妖";

        // ========== ⑥ 操作区 右下 ==========
        var actions = CreateEmpty("HUD_Actions", canvasGo.transform);
        // 卡牌缩小后，结束回合可略下移，更贴手牌区
        Stretch(actions, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-24, 100), new Vector2(200, 60));

        var endBtnGo = CreatePanel("Btn_EndTurn", actions.transform, BtnBg);
        FullStretch(endBtnGo);
        // 金边
        var outline = endBtnGo.AddComponent<Outline>();
        outline.effectColor = AccentGold;
        outline.effectDistance = new Vector2(2, -2);

        var endBtn = endBtnGo.AddComponent<Button>();
        var endColors = endBtn.colors;
        endColors.normalColor = Color.white;
        endColors.highlightedColor = new Color(1.1f, 1.05f, 0.95f, 1f);
        endColors.pressedColor = new Color(0.85f, 0.8f, 0.7f, 1f);
        endBtn.colors = endColors;
        endBtn.targetGraphic = endBtnGo.GetComponent<Image>();

        var endLabel = CreateTmp("Label", endBtnGo.transform, font, 30, TextMain, TextAlignmentOptions.Center);
        FullStretch(endLabel);
        endLabel.GetComponent<TextMeshProUGUI>().text = "结束回合";

        var tm = Object.FindAnyObjectByType<TurnManager>();
        if (tm != null)
        {
            while (endBtn.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(endBtn.onClick, 0);
            UnityEventTools.AddPersistentListener(endBtn.onClick, tm.EndPlayerTurn);
        }

        // ========== 胜负面板 ==========
        var resultOverlay = CreatePanel("HUD_Result", canvasGo.transform, new Color(0, 0, 0, 0.72f));
        FullStretch(resultOverlay);
        // 去掉全屏挡住射线时保留
        resultOverlay.GetComponent<Image>().raycastTarget = true;

        var resultPanel = CreatePanel("Panel", resultOverlay.transform, new Color(0.1f, 0.09f, 0.12f, 0.96f));
        Stretch(resultPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(460, 260));
        var rOutline = resultPanel.AddComponent<Outline>();
        rOutline.effectColor = AccentGold;
        rOutline.effectDistance = new Vector2(2, -2);

        var resultText = CreateTmp("ResultText", resultPanel.transform, font, 42, TextMain, TextAlignmentOptions.Center);
        Stretch(resultText, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400, 80));
        resultText.GetComponent<TextMeshProUGUI>().text = "封印成功！";

        var restartGo = CreatePanel("Btn_Restart", resultPanel.transform, BtnBg);
        Stretch(restartGo, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200, 56));
        var rBtnOutline = restartGo.AddComponent<Outline>();
        rBtnOutline.effectColor = AccentGold;
        rBtnOutline.effectDistance = new Vector2(1.5f, -1.5f);
        var restartBtn = restartGo.AddComponent<Button>();
        restartBtn.targetGraphic = restartGo.GetComponent<Image>();
        var restartLabel = CreateTmp("Label", restartGo.transform, font, 26, TextMain, TextAlignmentOptions.Center);
        FullStretch(restartLabel);
        restartLabel.GetComponent<TextMeshProUGUI>().text = "再战一次";

        // BattleResultUI 挂在 Canvas（常驻激活），panel 为 HUD_Result
        var bru = canvasGo.AddComponent<BattleResultUI>();
        bru.panelRoot = resultOverlay;
        bru.resultText = resultText.GetComponent<TextMeshProUGUI>();
        bru.restartButton = restartBtn;
        bru.winText = "封印成功！";
        bru.loseText = "封印失败…";
        bru.turnManager = tm;
        resultOverlay.SetActive(false);

        // 绑定敌人/玩家 stats
        if (tm != null)
        {
            enemyHb.characterStats = tm.enemyStats;
            if (enemyHb.characterStats == null)
            {
                var ellen = GameObject.Find("Ellen_skin (2)");
                if (ellen != null) enemyHb.characterStats = ellen.GetComponent<CharacterStats>();
            }
            psu.characterStats = tm.playerStats;
        }

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[BuildBattleHud] 横屏战斗 HUD 已按参考图重建并保存。");
        EditorUtility.DisplayDialog("重建HUD", "横屏战斗 HUD 已重建完成。\n请 Play 查看效果。", "OK");
    }

    // ---------- helpers ----------

    struct HpBar
    {
        public GameObject root;
        public Slider slider;
        public TextMeshProUGUI hpText;
        public Image fill;
    }

    static HpBar CreateHpBar(string name, Transform parent, TMP_FontAsset font, float width, float height)
    {
        var root = CreateEmpty(name, parent);
        var sliderGo = CreateEmpty("Slider", root.transform);
        FullStretch(sliderGo);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;

        var bg = CreatePanel("Background", sliderGo.transform, HpBg);
        FullStretch(bg);
        bg.GetComponent<Image>().raycastTarget = false;

        var fillArea = CreateEmpty("Fill Area", sliderGo.transform);
        FullStretch(fillArea);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.offsetMin = new Vector2(2, 2);
        fillAreaRt.offsetMax = new Vector2(-2, -2);

        var fill = CreatePanel("Fill", fillArea.transform, HpRed);
        FullStretch(fill);
        fill.GetComponent<Image>().raycastTarget = false;
        var fillImg = fill.GetComponent<Image>();

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImg;
        slider.direction = Slider.Direction.LeftToRight;

        var textGo = CreateTmp("HPText", root.transform, font, 18, TextMain, TextAlignmentOptions.Center);
        FullStretch(textGo);
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "0/0";
        tmp.raycastTarget = false;

        return new HpBar { root = root, slider = slider, hpText = tmp, fill = fillImg };
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return go;
    }

    static GameObject CreateEmpty(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
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
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        tmp.characterSpacing = 0f;
        tmp.lineSpacing = 0f;
        tmp.extraPadding = true;
        return go;
    }

    static void ConfigureTmp(TextMeshProUGUI tmp, string text, float size, bool wrap)
    {
        if (tmp == null) return;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
        tmp.richText = false;
        tmp.extraPadding = true;
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
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_FontAsset FindChineseFont()
    {
        // 优先 ziti / 2 / 3
        string[] names = { "ziti", "2", "3" };
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        TMP_FontAsset fallback = null;
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa == null) continue;
            for (int n = 0; n < names.Length; n++)
            {
                if (fa.name == names[n]) return fa;
            }
            if (fallback == null && path.Contains("Utilities"))
                fallback = fa;
        }
        return fallback;
    }
}
