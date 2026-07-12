using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 创建三关战役数据、奖励卡、符匣、UI，并挂到场景。
/// 菜单：AR封妖 / 配置三关战役
/// </summary>
public static class SetupCampaign
{
    const string DataRoot = "Assets/Game Data";
    const string StageFolder = "Assets/Game Data/Stages";
    const string CharFolder = "Assets/Game Data";
    const string CardFolder = "Assets/Game Data/Card Data";
    const string LibFolder = "Assets/Game Data/Card Library";

    [MenuItem("AR封妖/配置三关战役")]
    public static void Setup()
    {
        EnsureFolders();

        var attack = LoadCard("attack");
        var defense = LoadCard("defense");
        var qi = LoadCard("hp");
        var brk = LoadCard("break");
        var seal = LoadCard("seal");
        if (attack == null)
        {
            EditorUtility.DisplayDialog("战役", "缺少 attack.asset", "OK");
            return;
        }
        var fire = EnsureFireCard(attack);

        // ---- 奖励卡 ----
        var rLian = CreateReward("reward_lianzhan", "连斩符", CardType.Attack, WeaknessType.RedAttack,
            5, 0, 1, "造成5点伤害；命中红破绽可QTE", attack.cardImage);
        var rHuo = CreateReward("reward_lianhuo", "炼火符", CardType.Fire, WeaknessType.None,
            3, 0, 1, "造成3点伤害，附加2层灼烧", attack.cardImage, CardSpecialEffect.ApplyBurn, 2);
        var rLing = CreateReward("reward_zhenhunling", "镇魂铃", CardType.Seal, WeaknessType.PurpleSeal,
            3, 0, 1, "造成3点伤害；命中紫点可打断", attack.cardImage);
        var rPozhen = CreateReward("reward_pozhen", "破阵斩", CardType.Attack, WeaknessType.RedAttack,
            10, 0, 2, "造成10点伤害", attack.cardImage);
        var rYinhuo = CreateReward("reward_yinhuo", "引火诀", CardType.Fire, WeaknessType.None,
            0, 0, 1, "引爆敌人身上的灼烧；每层造成3点伤害，然后清除灼烧", attack.cardImage, CardSpecialEffect.DetonateBurn, 3);
        var rDing = CreateReward("reward_dinghun", "定魂符", CardType.Seal, WeaknessType.PurpleSeal,
            8, 0, 2, "重伤并打断蓄力", seal != null ? seal.cardImage : attack.cardImage);

        // ---- 敌人数据 ----
        var dataXiao = CreateEnemyData("enemy_xiaoyao", "小妖", 30, 0);
        var dataShi = CreateEnemyData("enemy_shiling", "石灵", 40, 6);
        var dataBoss = CreateEnemyData("enemy_shangui", "山鬼", 65, 0);

        // ---- 三套符匣 ----
        var fx1 = CreateFuXia("FuXia_XiaoYao", "小妖战符匣", 4, new List<CardDataSO>
        {
            attack, attack, defense, qi,
            attack, fire,
            defense, attack,
            brk, seal
        });
        var fx2 = CreateFuXia("FuXia_ShiLing", "石灵战符匣", 4, new List<CardDataSO>
        {
            brk, attack, defense, qi,
            attack, fire,
            defense, attack,
            attack, seal
        });
        var fx3 = CreateFuXia("FuXia_ShanGui", "山鬼战符匣", 4, new List<CardDataSO>
        {
            seal, attack, defense,
            brk, attack,
            qi,
            fire, defense,
            attack, attack
        });

        // ---- 关卡（意图/弱点对齐策划案 §10 三场战斗回合表）----
        var stage1 = CreateStage("Stage_01_XiaoYao", "第一关·小妖", "小妖", dataXiao, fx1,
            IntentXiaoYao(),
            new List<CardDataSO> { rLian, rHuo, rLing });
        var stage2 = CreateStage("Stage_02_ShiLing", "第二关·石灵", "石灵", dataShi, fx2,
            IntentShiLing(),
            new List<CardDataSO> { rPozhen, rYinhuo, rDing });
        var stage3 = CreateStage("Stage_03_ShanGui", "第三关·山鬼", "山鬼", dataBoss, fx3,
            IntentShanGui(),
            null);
        stage1.rewardInsertions = new List<BattleStageSO.RewardCardInsertion>();
        stage2.rewardInsertions = new List<BattleStageSO.RewardCardInsertion>
        {
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 8, earnedRewardIndex = 0 }
        };
        stage3.rewardInsertions = new List<BattleStageSO.RewardCardInsertion>
        {
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 3, earnedRewardIndex = 0 },
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 5, earnedRewardIndex = 1 }
        };
        EditorUtility.SetDirty(stage1);
        EditorUtility.SetDirty(stage2);
        EditorUtility.SetDirty(stage3);

        var campaign = AssetDatabase.LoadAssetAtPath<CampaignSO>(StageFolder + "/Campaign_Main.asset");
        if (campaign == null)
        {
            campaign = ScriptableObject.CreateInstance<CampaignSO>();
            AssetDatabase.CreateAsset(campaign, StageFolder + "/Campaign_Main.asset");
        }
        campaign.campaignName = "封妖试炼";
        campaign.stages = new List<BattleStageSO> { stage1, stage2, stage3 };
        EditorUtility.SetDirty(campaign);

        // ---- 场景对象 ----
        var flowGo = GameObject.Find("BattleFlowManager");
        if (flowGo == null) flowGo = new GameObject("BattleFlowManager");
        var flow = flowGo.GetComponent<BattleFlowManager>();
        if (flow == null) flow = flowGo.AddComponent<BattleFlowManager>();
        flow.campaign = campaign;
        flow.turnManager = Object.FindAnyObjectByType<TurnManager>();
        flow.cardDeck = Object.FindAnyObjectByType<CardDeck>();
        flow.resultUI = Object.FindAnyObjectByType<BattleResultUI>();
        flow.battleInfoUI = Object.FindAnyObjectByType<BattleInfoUI>();

        // 奖励 UI
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var old = canvas.transform.Find("HUD_Reward");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var rewardRoot = BuildRewardHud(canvas.transform);
            var rewardUI = rewardRoot.GetComponent<RewardSelectUI>();
            flow.rewardUI = rewardUI;
        }

        // 确保弱点仍在
        if (Object.FindAnyObjectByType<EnemyIntentController>() == null)
        {
            var ellen = GameObject.Find("Ellen_skin (2)");
            if (ellen != null)
                ellen.AddComponent<EnemyIntentController>();
        }

        EditorUtility.SetDirty(flowGo);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupCampaign] 三关战役配置完成。");
        EditorUtility.DisplayDialog("三关战役",
            "已配置：\n• 小妖(30HP) → 奖励三选一\n• 石灵(40HP/6甲) → 奖励三选一\n• 山鬼(65HP) Boss\n\n奖励会带入后续符匣。\nPlay 后由 BattleBootstrap 启动战役。",
            "OK");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(DataRoot))
            AssetDatabase.CreateFolder("Assets", "Game Data");
        if (!AssetDatabase.IsValidFolder(StageFolder))
            AssetDatabase.CreateFolder("Assets/Game Data", "Stages");
        if (!AssetDatabase.IsValidFolder(LibFolder))
            AssetDatabase.CreateFolder("Assets/Game Data", "Card Library");
    }

    static CardDataSO LoadCard(string name)
    {
        return AssetDatabase.LoadAssetAtPath<CardDataSO>($"{CardFolder}/{name}.asset");
    }

    static CardDataSO EnsureFireCard(CardDataSO attack)
    {
        var fire = LoadCard("fire");
        if (fire == null)
        {
            fire = ScriptableObject.CreateInstance<CardDataSO>();
            AssetDatabase.CreateAsset(fire, CardFolder + "/fire.asset");
        }
        fire.cardName = "烈火符";
        fire.cardImage = attack != null ? attack.cardImage : null;
        fire.cost = 1;
        fire.cardType = CardType.Fire;
        fire.description = "造成4点伤害，附加1层灼烧";
        fire.effectValue = 4;
        fire.effectValue2 = 0;
        fire.specialEffect = CardSpecialEffect.ApplyBurn;
        fire.specialEffectValue = 1;
        fire.weaknessTag = WeaknessType.None;
        EditorUtility.SetDirty(fire);
        return fire;
    }

    static CardDataSO CreateReward(string file, string cname, CardType type, WeaknessType tag,
        int effect, int e2, int cost, string desc, Sprite icon, CardSpecialEffect specialEffect = CardSpecialEffect.None, int specialEffectValue = 0)
    {
        string path = $"{CardFolder}/{file}.asset";
        var so = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<CardDataSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.cardName = cname;
        so.cardType = type;
        so.weaknessTag = tag;
        so.effectValue = effect;
        so.effectValue2 = e2;
        so.specialEffect = specialEffect;
        so.specialEffectValue = specialEffectValue;
        so.cost = cost;
        so.description = desc;
        if (icon != null) so.cardImage = icon;
        EditorUtility.SetDirty(so);
        return so;
    }

    static CharacterDataSO CreateEnemyData(string file, string cname, int hp, int armor)
    {
        string path = $"{CharFolder}/{file}.asset";
        var so = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<CharacterDataSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.characterName = cname;
        so.maxHP = hp;
        so.maxEnergy = 0;
        so.startArmor = armor;
        EditorUtility.SetDirty(so);
        return so;
    }

    static FuXiaOrderSO CreateFuXia(string file, string dname, int init, List<CardDataSO> cards)
    {
        string path = $"{LibFolder}/{file}.asset";
        var so = AssetDatabase.LoadAssetAtPath<FuXiaOrderSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<FuXiaOrderSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.displayName = dname;
        so.initialHandSize = init;
        so.orderedCards = cards;
        EditorUtility.SetDirty(so);
        return so;
    }

    static BattleStageSO CreateStage(string file, string sname, string ename, CharacterDataSO data,
        FuXiaOrderSO fuxia, List<EnemyIntentController.IntentStep> intents, List<CardDataSO> rewards)
    {
        string path = $"{StageFolder}/{file}.asset";
        var so = AssetDatabase.LoadAssetAtPath<BattleStageSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<BattleStageSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.stageName = sname;
        so.enemyDisplayName = ename;
        so.enemyData = data;
        so.fuXiaOrder = fuxia;
        so.intentLoop = intents;
        so.rewardChoices = rewards ?? new List<CardDataSO>();
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>策划案：小妖 — T1 攻5红 / T2 攻5无 / T3 攻7红，循环。</summary>
    static List<EnemyIntentController.IntentStep> IntentXiaoYao()
    {
        return new List<EnemyIntentController.IntentStep>
        {
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Attack, displayName = "普通攻击",
                exposedWeakness = WeaknessType.RedAttack, power = 5
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Attack, displayName = "普通攻击",
                exposedWeakness = WeaknessType.None, power = 5
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Attack, displayName = "攻击",
                exposedWeakness = WeaknessType.RedAttack, power = 7
            }
        };
    }

    /// <summary>石灵 — T1 防8黄 / T2 攻6红 / T3 防6黄 / T4 重击10红，循环。</summary>
    static List<EnemyIntentController.IntentStep> IntentShiLing()
    {
        return new List<EnemyIntentController.IntentStep>
        {
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Defend, displayName = "正在防御",
                exposedWeakness = WeaknessType.YellowArmor, armorGain = 8
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Attack, displayName = "普通攻击",
                exposedWeakness = WeaknessType.RedAttack, power = 6
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Defend, displayName = "正在防御",
                exposedWeakness = WeaknessType.YellowArmor, armorGain = 6
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Attack, displayName = "重击",
                exposedWeakness = WeaknessType.RedAttack, power = 10
            }
        };
    }

    /// <summary>
    /// 策划案：山鬼 — T1 攻8红 / T2 防8黄 / T3 蓄力紫 / T4 重击18无（未打断才结算），循环。
    /// 策划表第 5 回合「普通攻击 8 红」即循环回到 T1。
    /// </summary>
    static List<EnemyIntentController.IntentStep> IntentShanGui()
    {
        return new List<EnemyIntentController.IntentStep>
        {
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Attack, displayName = "普通攻击",
                exposedWeakness = WeaknessType.RedAttack, power = 8
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Defend, displayName = "正在防御",
                exposedWeakness = WeaknessType.YellowArmor, armorGain = 8
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Charge, displayName = "蓄力中",
                exposedWeakness = WeaknessType.PurpleSeal, power = 0
            },
            new EnemyIntentController.IntentStep
            {
                kind = EnemyIntentKind.Heavy, displayName = "重击",
                exposedWeakness = WeaknessType.None, power = 18
            }
        };
    }

    static GameObject BuildRewardHud(Transform canvas)
    {
        TMP_FontAsset font = null;
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        for (int i = 0; i < guids.Length; i++)
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (fa != null && (fa.name == "ziti" || fa.name == "2")) { font = fa; break; }
        }

        var root = new GameObject("HUD_Reward", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = 5;
        root.transform.SetParent(canvas, false);
        var rrt = root.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = 5;
        panel.transform.SetParent(root.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(720, 320);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.09f, 0.12f, 0.96f);

        var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        title.layer = 5;
        title.transform.SetParent(panel.transform, false);
        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = new Vector2(0, -16);
        trt.sizeDelta = new Vector2(-40, 40);
        var ttmp = title.GetComponent<TextMeshProUGUI>();
        if (font) ttmp.font = font;
        ttmp.fontSize = 28; ttmp.alignment = TextAlignmentOptions.Center;
        ttmp.color = Color.white; ttmp.text = "选择一张奖励符咒";

        var row = new GameObject("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.layer = 5;
        row.transform.SetParent(panel.transform, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.35f);
        rowRt.anchorMax = new Vector2(0.5f, 0.35f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(680, 180);
        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 20;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;

        var ui = root.AddComponent<RewardSelectUI>();
        ui.root = root;
        ui.titleText = ttmp;
        ui.buttonContainer = row.transform;
        // 预建 3 个按钮槽（Show 时会 Ensure）
        root.SetActive(false);
        return root;
    }
}
