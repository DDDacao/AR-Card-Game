using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 三关流程：开战 → 胜利 →（奖励三选一）→ 下一关 → 全通关 / 失败重试
/// </summary>
public class BattleFlowManager : MonoBehaviour
{
    public static BattleFlowManager Instance { get; private set; }

    [Header("战役")]
    public CampaignSO campaign;

    [Header("引用")]
    public TurnManager turnManager;
    public CardDeck cardDeck;
    public BattleResultUI resultUI;
    public RewardSelectUI rewardUI;
    public BattleInfoUI battleInfoUI;

    [Header("本局获得的奖励符（带入后续符匣）")]
    public List<CardDataSO> earnedRewards = new List<CardDataSO>();

    public int CurrentStageIndex { get; private set; }
    public BattleStageSO CurrentStage { get; private set; }
    public bool CampaignCompleted { get; private set; }

    private bool subscribed;

    private void Awake()
    {
        Instance = this;
        ResolveRefs();
        Subscribe();
    }

    private void Start()
    {
        ResolveRefs();
        Subscribe();

        // 由 BattleBootstrap 或本组件启动；若无 Bootstrap 则自动开战役
        if (FindAnyObjectByType<BattleBootstrap>() == null && campaign != null)
            StartCampaign();
    }

    private void ResolveRefs()
    {
        if (turnManager == null) turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
        if (cardDeck == null) cardDeck = FindAnyObjectByType<CardDeck>();
        if (resultUI == null) resultUI = FindAnyObjectByType<BattleResultUI>();
        if (rewardUI == null) rewardUI = FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include);
        if (battleInfoUI == null) battleInfoUI = FindAnyObjectByType<BattleInfoUI>();
    }

    private void OnDestroy()
    {
        if (turnManager != null && subscribed)
            turnManager.OnBattleEnded -= OnBattleEnded;
    }

    private void Subscribe()
    {
        if (subscribed || turnManager == null) return;
        turnManager.OnBattleEnded += OnBattleEnded;
        subscribed = true;
    }

    public void StartCampaign()
    {
        ResolveRefs();
        Subscribe();
        CampaignCompleted = false;
        earnedRewards.Clear();
        CurrentStageIndex = 0;
        if (campaign == null || campaign.stages == null || campaign.stages.Count == 0)
        {
            Debug.LogWarning("[BattleFlow] 无战役配置，回退单场战斗。");
            if (turnManager != null) turnManager.StartBattle();
            return;
        }
        StartStage(0);
    }

    public void RetryCurrentStage()
    {
        if (campaign == null || campaign.stages == null || campaign.stages.Count == 0)
        {
            turnManager?.RestartBattle();
            return;
        }
        StartStage(CurrentStageIndex);
    }

    public void StartStage(int index)
    {
        Subscribe();
        if (campaign == null || index < 0 || index >= campaign.stages.Count)
        {
            Debug.LogError("[BattleFlow] 关卡索引无效: " + index);
            return;
        }

        CurrentStageIndex = index;
        CurrentStage = campaign.stages[index];
        CampaignCompleted = false;

        ApplyStageToBattle(CurrentStage);
        Debug.Log($"[BattleFlow] 进入关卡 {index + 1}/{campaign.stages.Count}：{CurrentStage.stageName}");
        turnManager.StartBattle();
    }

    private void ApplyStageToBattle(BattleStageSO stage)
    {
        if (stage == null || turnManager == null) return;

        // 敌人显示名
        if (battleInfoUI != null)
            battleInfoUI.enemyDisplayName = stage.enemyDisplayName;

        // 敌人数值
        if (turnManager.enemyStats != null && stage.enemyData != null)
        {
            turnManager.enemyStats.templateData = stage.enemyData;
        }

        // 符匣：本关顺序 + 已获奖励插在开局序列前（奖励进符匣头部，开局更容易摸到）
        if (cardDeck != null)
        {
            cardDeck.useFixedOrder = true;
            cardDeck.fuXiaOrder = stage.fuXiaOrder;
            cardDeck.runtimePrefixCards = new List<CardDataSO>(earnedRewards);
        }

        // 意图
        turnManager.ResolveReferences();
        if (turnManager.enemyIntent != null)
        {
            if (stage.intentLoop != null && stage.intentLoop.Count > 0)
            {
                turnManager.enemyIntent.intentLoop = new List<EnemyIntentController.IntentStep>(stage.intentLoop);
            }
            turnManager.enemyIntent.RefreshWeaknessList();
        }
    }

    private void OnBattleEnded(bool playerWon)
    {
        if (playerWon)
            HandleVictory();
        else
            HandleDefeat();
    }

    private void HandleVictory()
    {
        ResolveRefs();
        Debug.Log($"[BattleFlow] 关卡胜利：{CurrentStage?.stageName}，hasRewards={CurrentStage != null && CurrentStage.HasRewards}");

        // 还有奖励可选
        if (CurrentStage != null && CurrentStage.HasRewards)
        {
            // 隐藏胜负面板，只显示奖励
            if (resultUI != null)
                resultUI.HidePanel();
            if (rewardUI != null)
            {
                // 提到 Canvas 最上层
                if (rewardUI.root != null)
                    rewardUI.root.transform.SetAsLastSibling();
                rewardUI.Show(CurrentStage.rewardChoices, OnRewardPicked);
                return;
            }
            // 无 UI 则自动拿第一张
            if (CurrentStage.rewardChoices.Count > 0)
                earnedRewards.Add(CurrentStage.rewardChoices[0]);
            ProceedAfterReward();
            return;
        }

        // Boss 等无奖励关
        ProceedAfterReward();
    }

    private void OnRewardPicked(CardDataSO card)
    {
        if (card != null)
        {
            earnedRewards.Add(card);
            Debug.Log($"[BattleFlow] 获得奖励：{card.cardName}");
        }
        if (resultUI != null)
            resultUI.HidePanel();
        ProceedAfterReward();
    }

    private void ProceedAfterReward()
    {
        int next = CurrentStageIndex + 1;
        if (campaign != null && next < campaign.stages.Count)
        {
            if (resultUI != null)
            {
                resultUI.ShowMessage($"准备迎战：{campaign.stages[next].enemyDisplayName}", true, "进入下一关", () =>
                {
                    resultUI.HidePanel();
                    StartStage(next);
                });
            }
            else
            {
                StartStage(next);
            }
            return;
        }

        // 全通关
        CampaignCompleted = true;
        if (resultUI != null)
        {
            resultUI.ShowMessage("三关尽破，封妖成功！", true, "再来一局", () =>
            {
                resultUI.HidePanel();
                StartCampaign();
            });
        }
    }

    private void HandleDefeat()
    {
        if (resultUI != null)
        {
            resultUI.ShowMessage("封印失败…", true, "重试本关", () =>
            {
                resultUI.HidePanel();
                RetryCurrentStage();
            });
        }
    }
}
