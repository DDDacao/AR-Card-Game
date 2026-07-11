using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 符匣固定补牌顺序。不洗牌：从前往后依次发给玩家。
/// 前 initialHandSize 张用于开局手牌，之后每回合从剩余序列补牌。
/// </summary>
[CreateAssetMenu(fileName = "FuXiaOrder", menuName = "Card/FuXiaOrderSO")]
public class FuXiaOrderSO : ScriptableObject
{
    [Header("显示名（如：小妖战符匣）")]
    public string displayName = "默认符匣";

    [Header("固定顺序（从左到右 / 从上到下依次抽出）")]
    public List<CardDataSO> orderedCards = new List<CardDataSO>();

    [Header("开局手牌张数（从序列头部取）")]
    public int initialHandSize = 4;

    /// <summary>
    /// 复制一份运行时队列（避免改到 SO 资产）
    /// </summary>
    public List<CardDataSO> CreateRuntimeQueue()
    {
        var list = new List<CardDataSO>();
        if (orderedCards == null) return list;
        for (int i = 0; i < orderedCards.Count; i++)
        {
            if (orderedCards[i] != null)
                list.Add(orderedCards[i]);
        }
        return list;
    }

    public int TotalCount
    {
        get
        {
            if (orderedCards == null) return 0;
            int n = 0;
            for (int i = 0; i < orderedCards.Count; i++)
                if (orderedCards[i] != null) n++;
            return n;
        }
    }
}
