using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单关配置：敌人数值、符匣、意图、通关奖励三选一
/// </summary>
[CreateAssetMenu(fileName = "BattleStage", menuName = "Campaign/BattleStageSO")]
public class BattleStageSO : ScriptableObject
{
    public string stageName = "小妖";
    public string enemyDisplayName = "小妖";

    [Header("敌人数值")]
    public CharacterDataSO enemyData;

    [Header("符匣固定顺序")]
    public FuXiaOrderSO fuXiaOrder;

    [Header("意图循环（空则用 EnemyIntentController 默认）")]
    public List<EnemyIntentController.IntentStep> intentLoop = new List<EnemyIntentController.IntentStep>();

    [Header("通关后奖励（三选一，Boss 关可留空）")]
    public List<CardDataSO> rewardChoices = new List<CardDataSO>();

    public bool HasRewards => rewardChoices != null && rewardChoices.Count > 0;
}
