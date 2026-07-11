using UnityEngine;
using TMPro;

/// <summary>
/// 参考图：右中提示 + 顶栏敌人名/意图
/// </summary>
public class BattleInfoUI : MonoBehaviour
{
    public TurnManager turnManager;
    public CardDeck cardDeck;

    public TextMeshProUGUI turnText;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI enemyIntentText;
    public TextMeshProUGUI enemyNameText;

    [Header("默认文案")]
    public string enemyDisplayName = "小妖";

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
    }

    private void Update()
    {
        if (deckCountText != null && cardDeck != null)
            deckCountText.text = $"符匣剩余：{cardDeck.DrawDeckCount}";
    }

    public void Refresh()
    {
        if (turnManager == null) return;

        if (turnText != null)
        {
            turnText.text = turnManager.IsBattleActive
                ? $"第 {turnManager.CurrentTurnIndex} 回合"
                : "等待开战";
        }

        if (deckCountText != null && cardDeck != null)
            deckCountText.text = $"符匣剩余：{cardDeck.DrawDeckCount}";

        if (enemyIntentText != null)
            enemyIntentText.text = string.IsNullOrEmpty(turnManager.CurrentEnemyIntent)
                ? "—"
                : turnManager.CurrentEnemyIntent;

        if (enemyNameText != null && !string.IsNullOrEmpty(enemyDisplayName))
            enemyNameText.text = enemyDisplayName;

        if (hintText != null)
        {
            if (!turnManager.IsBattleActive)
                hintText.text = turnManager.BattleEnded ? "战斗结束" : "扫描封妖阵开始";
            else if (turnManager.IsPlayerTurn)
                hintText.text = "拖出符咒攻击或防御\n点击结束回合";
            else
                hintText.text = "妖怪行动中…";
        }
    }
}
