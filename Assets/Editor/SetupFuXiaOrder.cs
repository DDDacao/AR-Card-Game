using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 创建「小妖战」符匣固定顺序并挂到 CardDeck。
/// 菜单：AR封妖 / 配置符匣固定顺序
/// </summary>
public static class SetupFuXiaOrder
{
    const string Folder = "Assets/Game Data/Card Library";
    const string Path = Folder + "/FuXia_XiaoYao.asset";

    [MenuItem("AR封妖/配置符匣固定顺序")]
    public static void Setup()
    {
        var attack = LoadCard("attack");
        var defense = LoadCard("defense");
        var qi = LoadCard("hp");
        var brk = LoadCard("break");
        var seal = LoadCard("seal");

        if (attack == null || defense == null || qi == null)
        {
            EditorUtility.DisplayDialog("符匣", "缺少基础卡牌资产（attack/defense/hp）", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Game Data"))
            AssetDatabase.CreateFolder("Assets", "Game Data");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Game Data", "Card Library");

        var order = AssetDatabase.LoadAssetAtPath<FuXiaOrderSO>(Path);
        if (order == null)
        {
            order = ScriptableObject.CreateInstance<FuXiaOrderSO>();
            AssetDatabase.CreateAsset(order, Path);
        }

        order.displayName = "小妖战符匣";
        order.initialHandSize = 4;
        // 教学固定流（对齐策划案思路，使用现有牌）：
        // 开局：斩妖、斩妖、护身、聚气  → 能打红、能防、懂灵气
        // 第2回补：破煞、斩妖          → 引入黄破甲
        // 第3回补：镇魂、护身          → 引入紫打断
        // 第4回补：斩妖、破煞          → 后续输出
        order.orderedCards = new List<CardDataSO>
        {
            attack, attack, defense, qi,           // 0-3 开局
            brk != null ? brk : attack, attack,    // 4-5
            seal != null ? seal : attack, defense, // 6-7
            attack, brk != null ? brk : attack     // 8-9
        };

        EditorUtility.SetDirty(order);

        var deck = Object.FindAnyObjectByType<CardDeck>();
        if (deck != null)
        {
            deck.fuXiaOrder = order;
            deck.useFixedOrder = true;
            EditorUtility.SetDirty(deck);
        }
        else
        {
            Debug.LogWarning("[SetupFuXiaOrder] 场景中无 CardDeck，请手动拖入 FuXia_XiaoYao。");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        string preview = "顺序预览：\n";
        for (int i = 0; i < order.orderedCards.Count; i++)
        {
            var c = order.orderedCards[i];
            string mark = i < order.initialHandSize ? "[开局]" : "[补牌]";
            string cname = c != null ? c.cardName : "?";
            preview += "  " + (i + 1) + ". " + mark + " " + cname + "\n";
        }

        Debug.Log("[SetupFuXiaOrder] 符匣固定顺序已配置。\n" + preview);
        EditorUtility.DisplayDialog("符匣固定顺序",
            "已创建并绑定：\n" + Path + "\n\n" + preview +
            "\n规则：不洗牌、用后消耗、每回补 2 张、手牌上限 6。",
            "OK");
    }

    static CardDataSO LoadCard(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<CardDataSO>($"Assets/Game Data/Card Data/{fileName}.asset");
    }
}
