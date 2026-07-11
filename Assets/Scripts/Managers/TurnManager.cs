using UnityEngine;
using System;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("角色属性")]
    public CharacterStats playerStats;
    public CharacterStats enemyStats;

    [Header("发牌器")]
    public CardDeck cardDeck;

    [Header("敌人意图（可空，自动在敌人身上找）")]
    public EnemyIntentController enemyIntent;

    [Header("战斗数值配置")]
    public int enemyAttackDamage = 5;
    public int initialDrawAmount = 4;
    public int drawPerTurn = 2;
    public string defaultEnemyIntent = "普通攻击";

    [Header("状态")]
    [SerializeField] private bool isBattleActive;
    [SerializeField] private bool isPlayerTurn = true;
    [SerializeField] private int currentTurnIndex;
    [SerializeField] private string currentEnemyIntent = "普通攻击";
    [SerializeField] private bool battleEnded;

    public bool IsBattleActive => isBattleActive;
    public bool IsPlayerTurn => isPlayerTurn;
    public int CurrentTurnIndex => currentTurnIndex;
    public string CurrentEnemyIntent => currentEnemyIntent;
    public bool BattleEnded => battleEnded;

    public event Action OnBattleStarted;
    public event Action OnPlayerTurnStarted;
    public event Action OnEnemyTurnStarted;
    public event Action OnTurnInfoChanged;
    public event Action<bool> OnBattleEnded;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ResolveReferences();
        if (FindAnyObjectByType<BattleBootstrap>() == null)
        {
            StartBattle();
        }
    }

    public void ResolveReferences()
    {
        if (playerStats == null)
        {
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo == null) playerGo = GameObject.Find("PlayerManager");
            if (playerGo != null) playerStats = playerGo.GetComponent<CharacterStats>();
        }

        if (enemyStats == null)
        {
            CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var stat in allStats)
            {
                if (stat.gameObject.name != "Player" && stat.gameObject.name != "PlayerManager")
                {
                    enemyStats = stat;
                    break;
                }
            }
        }

        if (enemyIntent == null && enemyStats != null)
        {
            enemyIntent = enemyStats.GetComponent<EnemyIntentController>();
            if (enemyIntent == null)
                enemyIntent = enemyStats.GetComponentInChildren<EnemyIntentController>();
            if (enemyIntent == null)
                enemyIntent = enemyStats.gameObject.AddComponent<EnemyIntentController>();
        }

        if (cardDeck == null)
            cardDeck = FindAnyObjectByType<CardDeck>();
    }

    public void StartBattle()
    {
        StopAllCoroutines();
        ResolveReferences();

        battleEnded = false;
        isBattleActive = true;
        isPlayerTurn = true;
        currentTurnIndex = 0;
        currentEnemyIntent = defaultEnemyIntent;

        if (playerStats != null) playerStats.InitializeStats();
        if (enemyStats != null) enemyStats.InitializeStats();

        if (enemyIntent != null)
        {
            enemyIntent.stats = enemyStats;
            // 战役关卡可覆写意图表（ApplyStage 已写入 intentLoop）
            enemyIntent.RefreshWeaknessList();
            enemyIntent.ResetIntent();
            currentEnemyIntent = enemyIntent.CurrentDisplayName;
        }

        if (cardDeck != null)
        {
            cardDeck.ClearAllForBattleReset();
            cardDeck.InitializeDeck();
            // 符匣固定顺序：开局张数由 FuXiaOrderSO.initialHandSize 决定
            cardDeck.DrawInitialHand(initialDrawAmount);
        }

        currentTurnIndex = 1;
        BeginPlayerTurnCore(false);
        OnBattleStarted?.Invoke();
        OnTurnInfoChanged?.Invoke();
        Debug.Log("[TurnManager] 战斗开始！");
    }

    public void StopBattle()
    {
        isBattleActive = false;
        StopAllCoroutines();
    }

    private void BeginPlayerTurnCore(bool drawCards)
    {
        if (battleEnded || !isBattleActive) return;

        isPlayerTurn = true;
        Debug.Log($"[TurnManager] 玩家回合开始（第 {currentTurnIndex} 回合）");

        if (playerStats != null)
            playerStats.ResetEnergy();

        // 展示本回合敌人意图与对应弱点
        if (enemyIntent != null)
        {
            enemyIntent.PresentIntentForPlayerTurn();
            currentEnemyIntent = enemyIntent.CurrentDisplayName;
        }
        else
        {
            currentEnemyIntent = defaultEnemyIntent;
        }

        if (drawCards && cardDeck != null)
            cardDeck.DrawRespectingHandLimit(drawPerTurn);

        OnPlayerTurnStarted?.Invoke();
        OnTurnInfoChanged?.Invoke();
    }

    public void StartPlayerTurn()
    {
        BeginPlayerTurnCore(true);
    }

    public void EndPlayerTurn()
    {
        if (!isBattleActive || battleEnded) return;
        if (!isPlayerTurn) return;
        if (QTEManager.Instance != null && QTEManager.Instance.IsRunning)
        {
            Debug.LogWarning("[TurnManager] QTE 进行中，无法结束回合。");
            return;
        }

        isPlayerTurn = false;
        Debug.Log("[TurnManager] 玩家回合结束！");
        StartCoroutine(EnemyTurnCoroutine());
    }

    private IEnumerator EnemyTurnCoroutine()
    {
        if (battleEnded || !isBattleActive) yield break;

        Debug.Log("[TurnManager] 怪物回合开始！");
        OnEnemyTurnStarted?.Invoke();
        OnTurnInfoChanged?.Invoke();

        yield return new WaitForSeconds(1.2f);

        if (battleEnded || !isBattleActive) yield break;

        if (enemyStats != null && enemyStats.CurrentHP > 0)
        {
            if (enemyIntent != null)
            {
                enemyIntent.ExecuteAndAdvance(playerStats);
            }
            else if (playerStats != null)
            {
                playerStats.TakeDamage(enemyAttackDamage);
                enemyStats.ClearArmor();
            }
        }

        if (CheckBattleEnd()) yield break;

        yield return new WaitForSeconds(0.8f);

        if (battleEnded || !isBattleActive) yield break;

        currentTurnIndex++;
        StartPlayerTurn();
    }

    public void NotifyCharacterDied(CharacterStats stats)
    {
        if (battleEnded || !isBattleActive) return;
        CheckBattleEnd();
    }

    public bool CheckBattleEnd()
    {
        if (battleEnded) return true;

        if (enemyStats != null && enemyStats.CurrentHP <= 0)
        {
            EndBattle(true);
            return true;
        }

        if (playerStats != null && playerStats.CurrentHP <= 0)
        {
            EndBattle(false);
            return true;
        }

        return false;
    }

    private void EndBattle(bool playerWon)
    {
        if (battleEnded) return;
        battleEnded = true;
        isBattleActive = false;
        isPlayerTurn = false;
        StopAllCoroutines();

        Debug.Log(playerWon ? "[TurnManager] 封印成功！玩家胜利。" : "[TurnManager] 封印失败！玩家战败。");
        OnBattleEnded?.Invoke(playerWon);
        OnTurnInfoChanged?.Invoke();
    }

    public void RestartBattle()
    {
        StartBattle();
    }
}
