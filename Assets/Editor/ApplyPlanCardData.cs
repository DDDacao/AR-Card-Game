using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 按《AR封妖牌局》第一版玩法策划案重置基础牌、奖励牌和三关固定补牌顺序。
/// 菜单：AR封妖 / 按策划重置符匣与火符
/// </summary>
public static class ApplyPlanCardData
{
    private const string CardFolder = "Assets/Game Data/Card Data";
    private const string LibraryFolder = "Assets/Game Data/Card Library";
    private const string StageFolder = "Assets/Game Data/Stages";

    [MenuItem("AR封妖/按策划重置符匣与火符")]
    public static void Apply()
    {
        var attack = LoadCard("attack");
        var defense = LoadCard("defense");
        var armorBreak = LoadCard("break");
        var qi = LoadCard("hp");
        var seal = LoadCard("seal");
        if (attack == null || defense == null || armorBreak == null || qi == null || seal == null)
        {
            EditorUtility.DisplayDialog("按策划重置", "缺少基础卡（attack / defense / break / hp / seal），未执行修改。", "OK");
            return;
        }

        var fire = LoadCard("fire");
        if (fire == null)
        {
            fire = ScriptableObject.CreateInstance<CardDataSO>();
            AssetDatabase.CreateAsset(fire, CardFolder + "/fire.asset");
        }
        ConfigureCard(fire, "烈火符", attack.cardImage, 1, CardType.Fire, "造成4点伤害，附加1层灼烧", 4, 0,
            CardSpecialEffect.ApplyBurn, 1);

        var lianhuo = LoadCard("reward_lianhuo");
        var yinhuo = LoadCard("reward_yinhuo");
        if (lianhuo == null || yinhuo == null)
        {
            EditorUtility.DisplayDialog("按策划重置", "缺少 reward_lianhuo 或 reward_yinhuo，未执行修改。", "OK");
            return;
        }
        ConfigureCard(lianhuo, "炼火符", attack.cardImage, 1, CardType.Fire, "造成3点伤害，附加2层灼烧", 3, 0,
            CardSpecialEffect.ApplyBurn, 2);
        ConfigureCard(yinhuo, "引火诀", attack.cardImage, 1, CardType.Fire, "引爆敌人身上的灼烧；每层造成3点伤害，然后清除灼烧", 0, 0,
            CardSpecialEffect.DetonateBurn, 3);

        // 策划表：开局4张；每回合固定补2张。未列出的尾部基础牌仍保留，供长回合使用。
        SetFuXia("FuXia_XiaoYao", new List<CardDataSO>
        {
            attack, attack, defense, qi,
            attack, fire,
            defense, attack,
            armorBreak, seal
        });
        SetFuXia("FuXia_ShiLing", new List<CardDataSO>
        {
            armorBreak, attack, defense, qi,
            attack, fire,
            defense, attack,
            attack, seal
        });
        SetFuXia("FuXia_ShanGui", new List<CardDataSO>
        {
            seal, attack, defense,
            armorBreak, attack,
            qi,
            fire, defense,
            attack, attack
        });

        SetRewardInsertions("Stage_01_XiaoYao");
        SetRewardInsertions("Stage_02_ShiLing", new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 8, earnedRewardIndex = 0 });
        SetRewardInsertions("Stage_03_ShanGui",
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 3, earnedRewardIndex = 0 },
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 5, earnedRewardIndex = 1 });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ApplyPlanCardData] 已按策划案配置烈火符、炼火/引火奖励与三关固定补牌顺序。");
        EditorUtility.DisplayDialog("按策划重置", "已完成：\n• 补上基础卡：烈火符\n• 修正炼火符 / 引火诀\n• 重置三关固定补牌与奖励插入时机", "OK");
    }

    private static CardDataSO LoadCard(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<CardDataSO>($"{CardFolder}/{fileName}.asset");
    }

    private static void ConfigureCard(CardDataSO card, string name, Sprite image, int cost, CardType type, string description,
        int effectValue, int effectValue2, CardSpecialEffect specialEffect, int specialEffectValue)
    {
        card.cardName = name;
        card.cardImage = image;
        card.cost = cost;
        card.cardType = type;
        card.description = description;
        card.effectValue = effectValue;
        card.effectValue2 = effectValue2;
        card.specialEffect = specialEffect;
        card.specialEffectValue = specialEffectValue;
        card.weaknessTag = WeaknessType.None;
        EditorUtility.SetDirty(card);
    }

    private static void SetFuXia(string fileName, List<CardDataSO> cards)
    {
        var fuxia = AssetDatabase.LoadAssetAtPath<FuXiaOrderSO>($"{LibraryFolder}/{fileName}.asset");
        if (fuxia == null)
        {
            Debug.LogError("[ApplyPlanCardData] 缺少符匣：" + fileName);
            return;
        }
        fuxia.initialHandSize = 4;
        fuxia.orderedCards = cards;
        EditorUtility.SetDirty(fuxia);
    }

    private static void SetRewardInsertions(string stageFileName, params BattleStageSO.RewardCardInsertion[] insertions)
    {
        var stage = AssetDatabase.LoadAssetAtPath<BattleStageSO>($"{StageFolder}/{stageFileName}.asset");
        if (stage == null)
        {
            Debug.LogError("[ApplyPlanCardData] 缺少关卡：" + stageFileName);
            return;
        }
        stage.rewardInsertions = new List<BattleStageSO.RewardCardInsertion>(insertions);
        EditorUtility.SetDirty(stage);
    }
}
