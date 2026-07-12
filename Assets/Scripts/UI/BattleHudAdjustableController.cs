using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可在场景中直接调整的战斗 HUD 数值控制器。
/// 布局、美术层均由 HUD_ArtSkin_Adjustable 子物体保存。
/// Boss 血条：按比例缩放填充层（保留胶囊圆角），禁止用矩形 Mask 硬裁切出方角。
/// </summary>
public sealed class BattleHudAdjustableController : MonoBehaviour
{
    [Header("Boss 血条")]
    [Tooltip("FillMask_生命裁切：宽度按血量变化；内部 Fill 拉伸填满，整段椭圆被压缩而非切方。")]
    public RectTransform bossFillMask;
    public float bossFullWidth = 1160f;
    [Tooltip("可空：自动找 Mask 下的 RawImage 填充层")]
    public RawImage bossFillImage;

    [Header("玩家生命（从下向上）")]
    public RectTransform playerFillMask;
    public float playerFillFullHeight = 146f;
    public TextMeshProUGUI playerHpText;

    [Header("玩家数值")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI armorText;

    [Header("敌方状态")]
    [Tooltip("血条下的弱点图标。世界弱点特效已足够时建议关闭。")]
    public bool showHudWeaknessIcon = false;
    public RawImage weaknessImage;
    public RawImage burnImage;
    public TextMeshProUGUI burnCountText;
    public Texture weaknessRed;
    public Texture weaknessYellow;
    public Texture weaknessPurple;

    private TurnManager turnManager;
    private CharacterStats player;
    private CharacterStats enemy;
    private EnemyIntentController intent;
    private float nextRefresh;
    private bool bossFillLayoutReady;
    private float bossFillHeight = 286f;

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .08f;
        ResolveReferences();
        RefreshVisuals();
    }

    public void EndTurn()
    {
        if (turnManager == null) ResolveReferences();
        if (turnManager != null) turnManager.EndPlayerTurn();
    }

    private void ResolveReferences()
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
        if (enemy != null && bossFillMask != null)
        {
            float ratio = enemy.MaxHP > 0 ? Mathf.Clamp01((float)enemy.CurrentHP / enemy.MaxHP) : 0f;
            ApplyBossHpFill(ratio);
        }

        if (player != null)
        {
            if (playerFillMask != null)
            {
                float ratio = player.MaxHP > 0 ? Mathf.Clamp01((float)player.CurrentHP / player.MaxHP) : 0f;
                playerFillMask.sizeDelta = new Vector2(playerFillMask.sizeDelta.x, playerFillFullHeight * ratio);
            }
            if (playerHpText != null) playerHpText.text = $"{player.CurrentHP}/{player.MaxHP}";
            if (energyText != null) energyText.text = $"{player.CurrentEnergy}/{player.MaxEnergy}";
            if (armorText != null) armorText.text = player.CurrentArmor.ToString();
        }

        if (weaknessImage != null)
        {
            // 世界空间已有弱点特效时，默认不显示血条旁 HUD 弱点图标
            if (!showHudWeaknessIcon)
            {
                if (weaknessImage.gameObject.activeSelf)
                    weaknessImage.gameObject.SetActive(false);
            }
            else
            {
                bool show = intent != null && intent.CurrentWeakness != WeaknessType.None;
                weaknessImage.gameObject.SetActive(show);
                if (show)
                {
                    weaknessImage.texture = intent.CurrentWeakness == WeaknessType.YellowArmor ? weaknessYellow
                        : intent.CurrentWeakness == WeaknessType.PurpleSeal ? weaknessPurple
                        : weaknessRed;
                }
            }
        }

        int burn = enemy != null ? enemy.BurnStacks : 0;
        bool showBurn = burn > 0;
        if (burnImage != null) burnImage.gameObject.SetActive(showBurn);
        if (burnCountText != null)
        {
            burnCountText.gameObject.SetActive(showBurn);
            if (showBurn) burnCountText.text = $"x{burn}";
        }
    }

    /// <summary>
    /// Mask 宽度 = full * ratio，Fill 拉伸填满 Mask。
    /// 整条胶囊贴图被横向压缩，两端始终保持圆角，而不是被竖线切成方块。
    /// </summary>
    private void ApplyBossHpFill(float ratio)
    {
        EnsureBossFillLayout();

        ratio = Mathf.Clamp01(ratio);
        if (ratio <= 0.001f)
        {
            bossFillMask.gameObject.SetActive(false);
            return;
        }

        if (!bossFillMask.gameObject.activeSelf)
            bossFillMask.gameObject.SetActive(true);

        float w = bossFullWidth * ratio;
        bossFillMask.sizeDelta = new Vector2(Mathf.Max(2f, w), bossFillHeight);

        if (bossFillImage != null)
            bossFillImage.enabled = true;
    }

    private void EnsureBossFillLayout()
    {
        if (bossFillLayoutReady || bossFillMask == null) return;
        bossFillLayoutReady = true;

        if (bossFillMask.sizeDelta.y > 1f)
            bossFillHeight = bossFillMask.sizeDelta.y;

        // 从左往右缩短
        bossFillMask.pivot = new Vector2(0f, 0.5f);

        if (bossFillImage == null)
            bossFillImage = bossFillMask.GetComponentInChildren<RawImage>(true);

        if (bossFillImage != null)
        {
            var fillRt = bossFillImage.rectTransform;
            // 填满父 Mask：宽度变时整图缩放，保留椭圆两端
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.localScale = Vector3.one;
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            bossFillImage.raycastTarget = false;
        }
    }
}
