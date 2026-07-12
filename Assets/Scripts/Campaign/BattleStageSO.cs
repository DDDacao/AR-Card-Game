using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单关配置：敌人数值、符匣、意图、通关奖励三选一
/// </summary>
[CreateAssetMenu(fileName = "BattleStage", menuName = "Campaign/BattleStageSO")]
public class BattleStageSO : ScriptableObject
{
    [Serializable]
    public class RewardCardInsertion
    {
        [Tooltip("已从基础符匣抽出的牌数达到此值时，先插入奖励牌。")]
        [Min(0)] public int afterBaseCardsDrawn;
        [Tooltip("earnedRewards 中的下标：0 为第一张奖励，1 为第二张奖励。")]
        [Min(0)] public int earnedRewardIndex;
    }

    public string stageName = "小妖";
    public string enemyDisplayName = "小妖";

    [Header("敌人数值")]
    public CharacterDataSO enemyData;

    [Header("符匣固定顺序")]
    public FuXiaOrderSO fuXiaOrder;

    [Header("已获奖励的固定插入时机")]
    public List<RewardCardInsertion> rewardInsertions = new List<RewardCardInsertion>();

    [Header("意图/弱点回合表（按策划案配置；空则用 EnemyIntentController 默认）")]
    [Tooltip("每步独立配置行动与 exposedWeakness（可为 None=本回合无弱点）。循环播放。")]
    public List<EnemyIntentController.IntentStep> intentLoop = new List<EnemyIntentController.IntentStep>();

    [Header("通关后奖励（三选一，Boss 关可留空）")]
    public List<CardDataSO> rewardChoices = new List<CardDataSO>();

    public bool HasRewards => rewardChoices != null && rewardChoices.Count > 0;
}
