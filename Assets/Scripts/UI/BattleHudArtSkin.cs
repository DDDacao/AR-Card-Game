using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 仅负责战斗 HUD 的美术分层与数值展示。
/// 不接管 QTE、手牌、卡牌相机或战斗流程。
/// </summary>
public sealed class BattleHudArtSkin : MonoBehaviour
{
    private const float ArtWidth = 1756f;
    private const float ArtHeight = 2493f;

    private CharacterStats player;
    private CharacterStats enemy;
    private EnemyIntentController intent;
    private TurnManager turnManager;

    private RectTransform bossFillMask;
    private RectTransform playerFillMask;
    private RawImage weaknessImage;
    private RawImage burnImage;
    private TextMeshProUGUI burnCount;
    private TextMeshProUGUI playerHpText;
    private TextMeshProUGUI energyText;
    private TextMeshProUGUI armorText;
    private Texture2D weakRed;
    private Texture2D weakYellow;
    private Texture2D weakPurple;

    private float nextRefresh;

    private void Start()
    {
        Build();
        HideLegacyVisuals();
        RefreshReferences();
        RefreshVisuals();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.08f;
        RefreshReferences();
        RefreshVisuals();
    }

    private void Build()
    {
        Transform old = transform.Find("HUD_ArtSkin");
        if (old != null) Destroy(old.gameObject);

        var root = CreateRect("HUD_ArtSkin", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.SetSiblingIndex(Mathf.Max(0, transform.Find("HUD_QTE")?.GetSiblingIndex() ?? transform.childCount - 1));

        Texture2D bossFrame = Load("boss_hp_frame");
        Texture2D bossFill = Load("boss_hp_fill");
        Texture2D playerFrame = Load("player_hp_frame");
        Texture2D playerFill = Load("player_hp_fill");
        Texture2D armorFrame = Load("armor_frame");
        Texture2D armorFill = Load("armor_fill");
        weakRed = Load("weakness_red");
        weakYellow = Load("weakness_yellow");
        weakPurple = Load("weakness_purple");

        // Boss: 底图在下，上层以左至右裁切，形成常规血条缩短效果。
        RectTransform boss = CreateRect("BossHealth", root, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -74), new Vector2(1160, 286));
        CreateRaw("Frame", boss, bossFrame, Uv(80, 473, 1616, 400), Vector2.zero, boss.sizeDelta);
        bossFillMask = CreateMask("FillMask", boss, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(1160, 286), new Vector2(0, .5f));
        // Fill 拉伸填满 Mask：血量变短时整条胶囊缩放，保留圆角
        CreateRaw("Fill", bossFillMask, bossFill, Uv(188, 577, 1444, 200),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f));

        // 状态图标只显示当前弱点和灼伤层数；不会改动场景中的真实弱点或 QTE。
        weaknessImage = CreateRaw("Weakness", root, weakRed, Uv(250, 500, 1250, 1250), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(-92, -265), new Vector2(104, 104));
        burnImage = CreateRaw("Burn", root, Load("burn"), Uv(500, 500, 760, 1260), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(42, -265), new Vector2(84, 104));
        burnCount = CreateText("BurnCount", root, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(95, -278), new Vector2(74, 42), 31, TextAlignmentOptions.Left);

        // 玩家生命：用矩形遮罩自下向上裁掉红色上层，保留圆形边框。
        RectTransform hp = CreateRect("PlayerHealth", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(142, 194), new Vector2(244, 244));
        CreateRaw("Frame", hp, playerFrame, Uv(204, 453, 1400, 1428), Vector2.zero, hp.sizeDelta);
        playerFillMask = CreateMask("FillMask", hp, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -1), new Vector2(142, 146), new Vector2(.5f, 0));
        CreateRaw("Fill", playerFillMask, playerFill, Uv(480, 745, 812, 840), new Vector2(.5f, 0f), new Vector2(.5f, 0f), Vector2.zero, new Vector2(142, 146), new Vector2(.5f, 0f));
        playerHpText = CreateText("Value", hp, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(170, 64), 31, TextAlignmentOptions.Center);

        // 灵气使用同款底层做灰白圆盘，数值直接显示剩余/上限。
        RectTransform energy = CreateRect("PlayerEnergy", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(392, 194), new Vector2(214, 214));
        var energyFrame = CreateRaw("Frame", energy, playerFrame, Uv(204, 453, 1400, 1428), Vector2.zero, energy.sizeDelta);
        energyFrame.color = new Color(.82f, .82f, .82f, 1f);
        energyText = CreateText("Value", energy, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(150, 64), 31, TextAlignmentOptions.Center);

        // 护甲放在生命下方，保留蓝色美术层与数字。
        RectTransform armor = CreateRect("PlayerArmor", root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(142, 62), new Vector2(168, 168));
        CreateRaw("Frame", armor, armorFrame, Uv(204, 461, 1400, 1424), Vector2.zero, armor.sizeDelta);
        CreateRaw("Fill", armor, armorFill, Uv(484, 741, 804, 840), new Vector2(0, -1), new Vector2(96, 100));
        armorText = CreateText("Value", armor, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(120, 56), 30, TextAlignmentOptions.Center);

        // 回合结束美术本身含文字，按钮仍调用原有 TurnManager 入口。
        var end = CreateRaw("EndTurn", root, Load("end_turn"), Uv(212, 1149, 1296, 336), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-170, 132), new Vector2(272, 72));
        end.raycastTarget = true;
        var button = end.gameObject.AddComponent<Button>();
        button.targetGraphic = end;
        button.onClick.AddListener(EndTurn);
    }

    private void EndTurn()
    {
        if (turnManager == null) turnManager = TurnManager.Instance;
        if (turnManager != null) turnManager.EndPlayerTurn();
    }

    private void RefreshReferences()
    {
        turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
        if (turnManager == null) return;
        if (turnManager.playerStats != null) player = turnManager.playerStats;
        if (turnManager.enemyStats != null) enemy = turnManager.enemyStats;
        if (turnManager.enemyIntent != null) intent = turnManager.enemyIntent;
        if (intent == null && enemy != null) intent = enemy.GetComponentInChildren<EnemyIntentController>();
    }

    private void RefreshVisuals()
    {
        if (enemy != null)
            SetHorizontalFill(bossFillMask, enemy.CurrentHP, enemy.MaxHP, 1160);

        if (player != null)
        {
            float hp = player.MaxHP > 0 ? Mathf.Clamp01((float)player.CurrentHP / player.MaxHP) : 0f;
            playerFillMask.sizeDelta = new Vector2(142, Mathf.Max(0, 146 * hp));
            playerHpText.text = $"{player.CurrentHP}/{player.MaxHP}";
            energyText.text = $"{player.CurrentEnergy}/{player.MaxEnergy}";
            armorText.text = player.CurrentArmor.ToString();
        }

        // 世界空间弱点特效已足够，HUD 血条旁弱点图标永久关闭
        if (weaknessImage != null && weaknessImage.gameObject.activeSelf)
            weaknessImage.gameObject.SetActive(false);

        int burn = enemy != null ? enemy.BurnStacks : 0;
        bool hasBurn = burn > 0;
        burnImage.gameObject.SetActive(hasBurn);
        burnCount.gameObject.SetActive(hasBurn);
        if (hasBurn) burnCount.text = $"x{burn}";
    }

    private void HideLegacyVisuals()
    {
        string[] names = { "HUD_Enemy", "HUD_Player", "HUD_SideInfo", "HUD_Actions" };
        foreach (string name in names)
        {
            Transform root = transform.Find(name);
            if (root == null) continue;
            for (int i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);
            var image = root.GetComponent<Image>();
            if (image != null) image.enabled = false;
        }
    }

    private static void SetHorizontalFill(RectTransform mask, int current, int max, float fullWidth)
    {
        if (mask == null) return;
        float value = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        float h = mask.sizeDelta.y > 1f ? mask.sizeDelta.y : 286f;
        mask.sizeDelta = new Vector2(fullWidth * value, h);

        // 确保填充层拉伸到 Mask 内（缩放胶囊而非硬裁切成方条）
        var fill = mask.GetComponentInChildren<RawImage>(true);
        if (fill != null)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private static Texture2D Load(string id) => Resources.Load<Texture2D>($"BattleHudSkin/{id}");
    private static Rect Uv(float x, float y, float width, float height) => new Rect(x / ArtWidth, y / ArtHeight, width / ArtWidth, height / ArtHeight);

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
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

    private static RawImage CreateRaw(string name, Transform parent, Texture texture, Rect uv, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        RectTransform rect = CreateRect(name, parent, min, max, position, size);
        rect.pivot = pivot ?? new Vector2(.5f, .5f);
        var raw = rect.gameObject.AddComponent<RawImage>();
        raw.texture = texture;
        raw.uvRect = uv;
        raw.raycastTarget = false;
        return raw;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, min, max, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
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
