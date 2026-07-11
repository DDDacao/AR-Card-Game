using UnityEngine;
using System;
using System.Collections;
using FSM;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("状态机")]
    public BattleStateMachine StateMachine { get; private set; }

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
        StateMachine = new BattleStateMachine();
    }

    private void Start()
    {
        ResolveReferences();
        if (FindAnyObjectByType<BattleBootstrap>() == null)
        {
            StartBattle();
        }
    }

    private void Update()
    {
        if (StateMachine != null)
            StateMachine.Update();
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
        if (StateMachine == null)
            StateMachine = new BattleStateMachine();

        StateMachine.TransitionTo(new BattleStateInit(this));
        Debug.Log("[TurnManager] 战斗初始化（通过状态机启动）！");
    }

    public void StopBattle()
    {
        isBattleActive = false;
        StopAllCoroutines();
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

        StateMachine.TransitionTo(new BattleStateEnemyTurn(this));
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
        StateMachine.TransitionTo(new BattleStateEnd(this, playerWon));
    }

    public void RestartBattle()
    {
        StartBattle();
    }

    #region FSM 访问接口
    public void SetIsBattleActive(bool active) => isBattleActive = active;
    public void SetIsPlayerTurn(bool turn) => isPlayerTurn = turn;
    public void SetCurrentTurnIndex(int index) => currentTurnIndex = index;
    public void SetCurrentEnemyIntent(string intent) => currentEnemyIntent = intent;
    public void SetBattleEnded(bool ended) => battleEnded = ended;

    public void InvokeBattleStarted() => OnBattleStarted?.Invoke();
    public void InvokePlayerTurnStarted() => OnPlayerTurnStarted?.Invoke();
    public void InvokeEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();
    public void InvokeTurnInfoChanged() => OnTurnInfoChanged?.Invoke();
    public void InvokeBattleEnded(bool playerWon) => OnBattleEnded?.Invoke(playerWon);
    #endregion
}
