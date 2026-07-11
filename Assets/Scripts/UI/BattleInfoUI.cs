using UnityEngine;
using TMPro;

/// <summary>
/// 顶栏敌人名 / 意图 / 灼烧 + 右中提示
/// </summary>
public class BattleInfoUI : MonoBehaviour
{
    public TurnManager turnManager;
    public CardDeck cardDeck;
    public CharacterStats enemyStats;

    public TextMeshProUGUI turnText;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI enemyIntentText;
    public TextMeshProUGUI enemyNameText;

    [Header("状态（可选，无则不显示）")]
    public TextMeshProUGUI enemyBurnText;

    [Header("默认文案")]
    public string enemyDisplayName = "小妖";

    private CharacterStats boundEnemy;

    private void Start()
    {
        Bind();
    }

    public void Bind()
    {
        if (turnManager == null)
            turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
        if (cardDeck == null)
            cardDeck = FindAnyObjectByType<CardDeck>();
        if (enemyStats == null && turnManager != null)
            enemyStats = turnManager.enemyStats;

        if (turnManager != null)
        {
            turnManager.OnTurnInfoChanged -= Refresh;
            turnManager.OnBattleStarted -= Refresh;
            turnManager.OnPlayerTurnStarted -= Refresh;
            turnManager.OnEnemyTurnStarted -= Refresh;

            turnManager.OnTurnInfoChanged += Refresh;
            turnManager.OnBattleStarted += Refresh;
            turnManager.OnPlayerTurnStarted += Refresh;
            turnManager.OnEnemyTurnStarted += Refresh;
        }

        RebindEnemyEvents();

        if (enemyNameText != null)
            enemyNameText.text = enemyDisplayName;

        Refresh();
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnInfoChanged -= Refresh;
            turnManager.OnBattleStarted -= Refresh;
            turnManager.OnPlayerTurnStarted -= Refresh;
            turnManager.OnEnemyTurnStarted -= Refresh;
        }
        UnbindEnemyEvents();
    }

    private void Update()
    {
        if (deckCountText != null && cardDeck != null)
            deckCountText.text = $"符匣剩余：{cardDeck.DrawDeckCount}";
    }

    private void RebindEnemyEvents()
    {
        if (turnManager != null && turnManager.enemyStats != null)
            enemyStats = turnManager.enemyStats;

        if (boundEnemy == enemyStats) return;

        UnbindEnemyEvents();
        boundEnemy = enemyStats;
        if (boundEnemy != null)
            boundEnemy.OnBurnChanged += OnEnemyBurnChanged;
    }

    private void UnbindEnemyEvents()
    {
        if (boundEnemy != null)
            boundEnemy.OnBurnChanged -= OnEnemyBurnChanged;
        boundEnemy = null;
    }

    private void OnEnemyBurnChanged(int _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (turnManager == null) return;

        RebindEnemyEvents();

        if (turnText != null)
        {
            turnText.text = turnManager.IsBattleActive
                ? $"第 {turnManager.CurrentTurnIndex} 回合"
                : "等待开战";
        }

        if (deckCountText != null && cardDeck != null)
            deckCountText.text = $"符匣剩余：{cardDeck.DrawDeckCount}";

        // 意图：只显示意图本身，不夹带状态标签
        if (enemyIntentText != null)
        {
            enemyIntentText.richText = true;
            string intent = string.IsNullOrEmpty(turnManager.CurrentEnemyIntent)
                ? "—"
                : turnManager.CurrentEnemyIntent;
            enemyIntentText.text = intent;
            TmpChineseFontUtil.Apply(enemyIntentText, intent);
        }

        // 灼烧：独立一行，用颜色属性而不是把 <color> 当字符串硬塞
        RefreshBurnText();

        if (enemyNameText != null && !string.IsNullOrEmpty(enemyDisplayName))
        {
            enemyNameText.text = enemyDisplayName;
            TmpChineseFontUtil.Apply(enemyNameText, enemyDisplayName);
        }

        if (hintText != null)
        {
            if (!turnManager.IsBattleActive)
                hintText.text = turnManager.BattleEnded ? "战斗结束" : "扫描封妖阵开始";
            else if (turnManager.IsPlayerTurn)
                hintText.text = "拖出符咒攻击或防御\n点击结束回合";
            else
                hintText.text = "妖怪行动中…";
            TmpChineseFontUtil.Apply(hintText, hintText.text);
        }
    }

    private void RefreshBurnText()
    {
        if (enemyBurnText == null) return;

        int stacks = enemyStats != null ? enemyStats.BurnStacks : 0;
        if (stacks > 0)
        {
            enemyBurnText.gameObject.SetActive(true);
            enemyBurnText.richText = false;
            enemyBurnText.color = new Color(1f, 0.55f, 0.22f, 1f);
            string s = $"灼烧 {stacks} 层";
            enemyBurnText.text = s;
            TmpChineseFontUtil.Apply(enemyBurnText, s);
        }
        else
        {
            enemyBurnText.text = "";
            enemyBurnText.gameObject.SetActive(false);
        }
    }
}
