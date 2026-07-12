using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可在场景中直接调整的战斗 HUD 数值控制器。
/// 布局、美术层、裁切范围均由 HUD_ArtSkin_Adjustable 子物体保存，
/// 此组件只更新数值和当前状态图标，不会动态创建或销毁 UI。
/// </summary>
public sealed class BattleHudAdjustableController : MonoBehaviour
{
    [Header("Boss 血条")]
    [Tooltip("可直接调整该 RectTransform 的宽度；控制器按生命百分比裁切。")]
    public RectTransform bossFillMask;
    public float bossFullWidth = 1160f;

    [Header("玩家生命（从下向上）")]
    public RectTransform playerFillMask;
    public float playerFillFullHeight = 146f;
    public TextMeshProUGUI playerHpText;

    [Header("玩家数值")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI armorText;

    [Header("敌方状态")]
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
            bossFillMask.sizeDelta = new Vector2(bossFullWidth * ratio, bossFillMask.sizeDelta.y);
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
            bool show = intent != null && intent.CurrentWeakness != WeaknessType.None;
            weaknessImage.gameObject.SetActive(show);
            if (show)
            {
                weaknessImage.texture = intent.CurrentWeakness == WeaknessType.YellowArmor ? weaknessYellow
                    : intent.CurrentWeakness == WeaknessType.PurpleSeal ? weaknessPurple
                    : weaknessRed;
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
}
