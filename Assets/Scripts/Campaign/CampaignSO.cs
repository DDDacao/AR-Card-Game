using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 整条战役：按顺序打多个 BattleStage
/// </summary>
[CreateAssetMenu(fileName = "Campaign", menuName = "Campaign/CampaignSO")]
public class CampaignSO : ScriptableObject
{
    public string campaignName = "封妖试炼";
    public List<BattleStageSO> stages = new List<BattleStageSO>();
}
