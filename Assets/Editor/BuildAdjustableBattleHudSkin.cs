using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>把美术 HUD 落为可在 Hierarchy 里直接拖动的场景对象。</summary>
public static class BuildAdjustableBattleHudSkin
{
    private const float ArtWidth = 1756f;
    private const float ArtHeight = 2493f;
    private const string RootName = "HUD_ArtSkin_Adjustable";

    [MenuItem("AR封妖/搭建可手动调整的战斗HUD")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[AdjustableHUD] 未找到 Canvas。");
            return;
        }

        Transform old = canvas.transform.Find(RootName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var oldRuntimeSkin = canvas.GetComponent<BattleHudArtSkin>();
        if (oldRuntimeSkin != null) Object.DestroyImmediate(oldRuntimeSkin);

        Texture2D bossFrame = Load("boss_hp_frame");
        Texture2D bossFill = Load("boss_hp_fill");
        Texture2D playerFrame = Load("player_hp_frame");
        Texture2D playerFill = Load("player_hp_fill");
        Texture2D armorFrame = Load("armor_frame");
        Texture2D armorFill = Load("armor_fill");
        Texture2D weakRed = Load("weakness_red");
        Texture2D weakYellow = Load("weakness_yellow");
        Texture2D weakPurple = Load("weakness_purple");
        Texture2D burn = Load("burn");
        Texture2D endTurn = Load("end_turn");
        if (bossFrame == null || bossFill == null || playerFrame == null || playerFill == null || armorFrame == null || armorFill == null || weakRed == null || weakYellow == null || weakPurple == null || burn == null || endTurn == null)
        {
            Debug.LogError("[AdjustableHUD] 美术资源缺失，请确认 Assets/Resources/BattleHudSkin 完整。");
            return;
        }

        RectTransform root = CreateRect(RootName, canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Transform qte = canvas.transform.Find("HUD_QTE");
        root.SetSiblingIndex(qte != null ? qte.GetSiblingIndex() : canvas.transform.childCount - 1);
        BattleHudAdjustableController controller = root.gameObject.AddComponent<BattleHudAdjustableController>();

        RectTransform boss = CreateRect("BossHealth_可调", root, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -74), new Vector2(1160, 286));
        CreateRaw("Frame_底层", boss, bossFrame, Uv(80, 473, 1616, 400), Vector2.zero, boss.sizeDelta);
        // 血条填充：Mask 按血量改宽；Fill 拉伸填满 Mask，整条胶囊缩放保留圆角（勿固定宽 + 硬裁切）
        controller.bossFillMask = CreateMask("FillMask_生命裁切", boss, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(1160, 286), new Vector2(0, .5f));
        controller.bossFullWidth = 1160f;
        var bossFillRaw = CreateRaw("Fill_上层", controller.bossFillMask, bossFill, Uv(188, 577, 1444, 200),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f));
        controller.bossFillImage = bossFillRaw;

        controller.weaknessImage = CreateRaw("WeaknessIcon_Adjustable", root, weakRed, Uv(250, 500, 1250, 1250), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(-92, -265), new Vector2(104, 104), new Vector2(.5f, .5f));
        controller.burnImage = CreateRaw("BurnIcon_Adjustable", root, burn, Uv(500, 500, 760, 1260), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(42, -265), new Vector2(84, 104), new Vector2(.5f, .5f));
        controller.burnCountText = CreateText("BurnCount_可调", root, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(95, -278), new Vector2(74, 42), 31, TextAlignmentOptions.Left);

        RectTransform hp = CreateRect("PlayerHealth_可调", root, Vector2.zero, Vector2.zero, new Vector2(142, 194), new Vector2(244, 244));
        CreateRaw("Frame_底层", hp, playerFrame, Uv(204, 453, 1400, 1428), Vector2.zero, hp.sizeDelta);
        controller.playerFillMask = CreateMask("FillMask_由下向上裁切", hp, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -1), new Vector2(142, 146), new Vector2(.5f, 0));
        CreateRaw("Fill_上层", controller.playerFillMask, playerFill, Uv(480, 745, 812, 840), new Vector2(.5f, 0f), new Vector2(.5f, 0f), Vector2.zero, new Vector2(142, 146), new Vector2(.5f, 0f));
        controller.playerHpText = CreateText("Value_HP", hp, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(170, 64), 31, TextAlignmentOptions.Center);

        RectTransform energy = CreateRect("PlayerEnergy_可调", root, Vector2.zero, Vector2.zero, new Vector2(392, 194), new Vector2(214, 214));
        RawImage energyFrame = CreateRaw("Frame_灵气底层", energy, playerFrame, Uv(204, 453, 1400, 1428), Vector2.zero, energy.sizeDelta);
        energyFrame.color = new Color(.82f, .82f, .82f, 1f);
        controller.energyText = CreateText("Value_灵气", energy, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(150, 64), 31, TextAlignmentOptions.Center);

        RectTransform armor = CreateRect("PlayerArmor_可调", root, Vector2.zero, Vector2.zero, new Vector2(142, 62), new Vector2(168, 168));
        CreateRaw("Frame_护甲底层", armor, armorFrame, Uv(204, 461, 1400, 1424), Vector2.zero, armor.sizeDelta);
        CreateRaw("Fill_护甲上层", armor, armorFill, Uv(484, 741, 804, 840), new Vector2(0, -1), new Vector2(96, 100));
        controller.armorText = CreateText("Value_护甲", armor, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(120, 56), 30, TextAlignmentOptions.Center);

        RawImage end = CreateRaw("EndTurn_Adjustable", root, endTurn, Uv(212, 1149, 1296, 336), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-170, 132), new Vector2(272, 72), new Vector2(.5f, .5f));
        end.raycastTarget = true;
        Button button = end.gameObject.AddComponent<Button>();
        button.targetGraphic = end;
        UnityEventTools.AddPersistentListener(button.onClick, controller.EndTurn);

        controller.weaknessRed = weakRed;
        controller.weaknessYellow = weakYellow;
        controller.weaknessPurple = weakPurple;
        HideLegacyVisuals(canvas.transform);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[AdjustableHUD] 已创建 HUD_ArtSkin_Adjustable。请在 Canvas 下直接选择各个 *_可调 节点调整 RectTransform。");
    }

    [MenuItem("AR封妖/补充可调HUD文字框")]
    public static void AddInfoLabels()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[AdjustableHUD] 未找到 Canvas。");
            return;
        }

        Transform root = canvas.transform.Find(RootName);
        if (root == null)
        {
            Debug.LogError("[AdjustableHUD] 请先执行「搭建可手动调整的战斗HUD」。");
            return;
        }

        Transform old = root.Find("BattleInfo_Adjustable");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // 这些节点均是新增节点；不会重置或修改用户已调整的 HUD 节点。
        RectTransform infoRoot = CreateRect("BattleInfo_Adjustable", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BattleHudInfoPresenter presenter = infoRoot.gameObject.AddComponent<BattleHudInfoPresenter>();

        Transform boss = root.Find("BossHealth_可调");
        if (boss != null)
        {
            presenter.bossNameText = CreateText("BossName_可调", boss, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, 28), new Vector2(360, 48), 34, TextAlignmentOptions.Center);
            presenter.enemyIntentText = CreateText("EnemyIntent_可调", boss, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0, -34), new Vector2(520, 44), 25, TextAlignmentOptions.Center);
        }

        Transform endTurn = root.Find("EndTurn_Adjustable");
        if (endTurn != null)
            presenter.endTurnText = CreateText("Label_回合结束", endTurn, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(210, 54), 31, TextAlignmentOptions.Center);

        RectTransform side = CreateRect("TurnInfoPanel_可调", infoRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-208, -335), new Vector2(310, 210));
        presenter.turnStateText = CreateText("TurnState_可调", side, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -42), new Vector2(270, 88), 27, TextAlignmentOptions.Center);
        presenter.hintText = CreateText("Hint_可调", side, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0, 48), new Vector2(280, 92), 21, TextAlignmentOptions.Center);

        HideAllLegacyRoots(canvas.transform);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[AdjustableHUD] 已补充 Boss 名称、攻击意图、右侧回合提示与结束回合文字。可直接在 Canvas/HUD_ArtSkin_Adjustable 下微调。");
    }

    private static void HideLegacyVisuals(Transform canvas)
    {
        string[] names = { "HUD_Enemy", "HUD_Player", "HUD_SideInfo", "HUD_Actions" };
        foreach (string name in names)
        {
            Transform root = canvas.Find(name);
            if (root == null) continue;
            for (int i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);
            Image image = root.GetComponent<Image>();
            if (image != null) image.enabled = false;
        }
    }

    private static void HideAllLegacyRoots(Transform canvas)
    {
        string[] names = { "HUD_Enemy", "HUD_Player", "HUD_SideInfo", "HUD_Actions" };
        foreach (string name in names)
        {
            Transform root = canvas.Find(name);
            if (root != null) root.gameObject.SetActive(false);
        }
    }

    private static Texture2D Load(string id) => AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/BattleHudSkin/{id}.png");
    private static Rect Uv(float x, float y, float width, float height) => new Rect(x / ArtWidth, y / ArtHeight, width / ArtWidth, height / ArtHeight);

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform CreateMask(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = CreateRect(name, parent, min, max, position, size);
        rect.pivot = pivot;
        rect.gameObject.AddComponent<RectMask2D>();
        return rect;
    }

    private static RawImage CreateRaw(string name, Transform parent, Texture texture, Rect uv, Vector2 position, Vector2 size)
    {
        return CreateRaw(name, parent, texture, uv, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size, new Vector2(.5f, .5f));
    }

    private static RawImage CreateRaw(string name, Transform parent, Texture texture, Rect uv, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = CreateRect(name, parent, min, max, position, size);
        rect.pivot = pivot;
        RawImage raw = rect.gameObject.AddComponent<RawImage>();
        raw.texture = texture;
        raw.uvRect = uv;
        raw.raycastTarget = false;
        return raw;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, min, max, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.outlineWidth = .18f;
        text.outlineColor = Color.black;
        text.raycastTarget = false;
        return text;
    }
}
