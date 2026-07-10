using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("角色属性")]
    public CharacterStats playerStats;
    public CharacterStats enemyStats;

    [Header("发牌器")]
    public CardDeck cardDeck;

    [Header("战斗数值配置")]
    public int enemyAttackDamage = 6;
    public int drawCardAmount = 3;

    private bool isPlayerTurn = true;
    public bool IsPlayerTurn => isPlayerTurn;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 自动查找组件（如果面板没拖拽赋值）
        if (playerStats == null)
        {
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo == null) playerGo = GameObject.Find("PlayerManager");
            if (playerGo != null) playerStats = playerGo.GetComponent<CharacterStats>();
        }
        
        if (enemyStats == null)
        {
            CharacterStats[] allStats = FindObjectsByType<CharacterStats>();
            foreach (var stat in allStats)
            {
                if (stat.gameObject.name != "Player" && stat.gameObject.name != "PlayerManager")
                {
                    enemyStats = stat;
                    break;
                }
            }
        }

        if (cardDeck == null)
        {
            cardDeck = FindAnyObjectByType<CardDeck>();
        }

        // 确保发牌器在抽牌前，先完成卡组数据的初始化（防止执行顺序冲突导致抽牌为空）
        if (cardDeck != null)
        {
            cardDeck.InitializeDeck();
        }

        // 开启第一回合
        StartPlayerTurn();
    }

    /// <summary>
    /// 开始玩家回合
    /// </summary>
    public void StartPlayerTurn()
    {
        isPlayerTurn = true;
        Debug.Log("[TurnManager] 玩家回合开始！");

        // 1. 重置能量/灵气
        if (playerStats != null)
        {
            playerStats.ResetEnergy();
        }

        // 2. 玩家抽牌
        if (cardDeck != null)
        {
            cardDeck.DrawCard(drawCardAmount);
        }
    }

    /// <summary>
    /// 结束玩家回合（点击结束回合按钮调用）
    /// </summary>
    public void EndPlayerTurn()
    {
        if (!isPlayerTurn) return;

        isPlayerTurn = false;
        Debug.Log("[TurnManager] 玩家回合结束！");

        // 1. 弃掉所有手牌
        if (cardDeck != null)
        {
            cardDeck.DiscardHand();
        }

        // 2. 进入怪物回合（协程延时，体现怪物动作停顿）
        StartCoroutine(EnemyTurnCoroutine());
    }

    private IEnumerator EnemyTurnCoroutine()
    {
        Debug.Log("[TurnManager] 怪物回合开始！");
        yield return new WaitForSeconds(1.5f); // 停顿1.5秒，模拟思考时间

        // 1. 怪物反击
        if (enemyStats != null && playerStats != null)
        {
            // 确保怪物还活着
            if (enemyStats.CurrentHP > 0)
            {
                Debug.Log($"[TurnManager] {enemyStats.gameObject.name} 发起攻击，对玩家造成 {enemyAttackDamage} 点伤害！");
                playerStats.TakeDamage(enemyAttackDamage);
                
                // 怪物回合结束时，清除怪物身上的护甲（卡牌游戏常规设定）
                enemyStats.ClearArmor();
            }
        }

        yield return new WaitForSeconds(1.0f); // 动作完再停顿1秒

        // 2. 回到玩家回合
        StartPlayerTurn();
    }
}
