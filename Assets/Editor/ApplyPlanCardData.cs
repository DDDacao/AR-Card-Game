using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 按策划案重置：卡牌命名/贴图、三关固定补牌、奖励 UI 卡面。
/// 菜单：AR封妖 / 按策划重置符匣与火符
/// 菜单：AR封妖 / 命名卡牌并绑定贴图
/// 菜单：AR封妖 / 重建奖励三选一卡面UI
/// </summary>
public static class ApplyPlanCardData
{
    private const string CardFolder = "Assets/Game Data/Card Data";
    private const string LibraryFolder = "Assets/Game Data/Card Library";
    private const string StageFolder = "Assets/Game Data/Stages";
    private const string ArtFolder = "Assets/Prefabs/Cardpng";

    [MenuItem("AR封妖/按策划重置符匣与火符")]
    public static void Apply()
    {
        RenameCardAssetsForDebug();
        BindAllCardArtAndNames();

        var attack = LoadCard("attack", "斩妖符");
        var defense = LoadCard("defense");
        var armorBreak = LoadCard("break", "破煞符");
        var qi = LoadCard("hp");
        var seal = LoadCard("seal");
        var fire = LoadCard("fire");

        if (attack == null || defense == null || armorBreak == null || qi == null || seal == null)
        {
            EditorUtility.DisplayDialog("按策划重置",
                "缺少基础卡（attack/defense/break/hp/seal）。\n请先跑「命名卡牌并绑定贴图」。", "OK");
            return;
        }

        if (fire == null)
        {
            fire = ScriptableObject.CreateInstance<CardDataSO>();
            AssetDatabase.CreateAsset(fire, CardFolder + "/fire.asset");
        }

        ConfigureCard(fire, "烈火符", LoadSprite("烈火符卡牌"), 1, CardType.Fire,
            "造成4点伤害，附加1层灼烧", 4, 0, CardSpecialEffect.ApplyBurn, 1, WeaknessType.None);

        var lianhuo = LoadCard("reward_lianhuo");
        var yinhuo = LoadCard("reward_yinhuo");
        if (lianhuo != null)
            ConfigureCard(lianhuo, "炼火符", LoadSprite("炼火符卡牌"), 1, CardType.Fire,
                "造成3点伤害，附加2层灼烧", 3, 0, CardSpecialEffect.ApplyBurn, 2, WeaknessType.None);
        if (yinhuo != null)
            ConfigureCard(yinhuo, "引火诀", LoadSprite("引火符卡牌"), 1, CardType.Fire,
                "引爆敌人身上的灼烧；每层造成3点伤害，然后清除灼烧", 0, 0, CardSpecialEffect.DetonateBurn, 3, WeaknessType.None);

        // 策划案 V1.0 固定补牌（奖励插入由 Stage.rewardInsertions 处理）
        // 小妖：斩斩护聚 | 斩烈火 | 护斩 | 破镇（尾部填充）
        SetFuXia("FuXia_XiaoYao", "小妖战符匣", new List<CardDataSO>
        {
            attack, attack, defense, qi,
            attack, fire,
            defense, attack,
            armorBreak, seal
        });
        // 石灵：破斩护聚 | 斩烈火 | 护斩 | （奖励1插入后）斩镇
        SetFuXia("FuXia_ShiLing", "石灵战符匣", new List<CardDataSO>
        {
            armorBreak, attack, defense, qi,
            attack, fire,
            defense, attack,
            attack, seal
        });
        // 山鬼：镇斩护 | （奖1）| 破斩 | （奖2）| 聚 | 烈火护 | 斩斩
        SetFuXia("FuXia_ShanGui", "山鬼战符匣", new List<CardDataSO>
        {
            seal, attack, defense,
            armorBreak, attack,
            qi,
            fire, defense,
            attack, attack
        });

        SetRewardInsertions("Stage_01_XiaoYao");
        SetRewardInsertions("Stage_02_ShiLing",
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 8, earnedRewardIndex = 0 });
        SetRewardInsertions("Stage_03_ShanGui",
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 3, earnedRewardIndex = 0 },
            new BattleStageSO.RewardCardInsertion { afterBaseCardsDrawn = 5, earnedRewardIndex = 1 });

        // 同步 CardDeck 上的符匣引用到当前关（默认小妖）
        var deck = Object.FindAnyObjectByType<CardDeck>();
        if (deck != null)
        {
            var fx = AssetDatabase.LoadAssetAtPath<FuXiaOrderSO>(LibraryFolder + "/FuXia_XiaoYao.asset");
            if (fx != null)
            {
                deck.fuXiaOrder = fx;
                EditorUtility.SetDirty(deck);
            }
        }

        RebuildRewardHudPanel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        string preview = BuildOrderPreview();
        Debug.Log("[ApplyPlanCardData] 完成。\n" + preview);
        EditorUtility.DisplayDialog("按策划重置",
            "已完成：\n• 卡牌命名 + 贴图\n• 三关固定补牌顺序\n• 奖励插入时机\n• 奖励三选一卡面 UI\n\n" + preview,
            "OK");
    }

    [MenuItem("AR封妖/命名卡牌并绑定贴图")]
    public static void NameAndBindMenu()
    {
        RenameCardAssetsForDebug();
        BindAllCardArtAndNames();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("卡牌命名", "已为所有卡牌设置中文名并绑定 Cardpng 贴图。\n资源文件名已统一为英文 id。", "OK");
    }

    [MenuItem("AR封妖/重建奖励三选一卡面UI")]
    public static void RebuildRewardHudMenu()
    {
        RebuildRewardHudPanel();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        EditorUtility.DisplayDialog("奖励 UI", "已重建 HUD_Reward（带卡面图）。", "OK");
    }

    /// <summary>把中文文件名统一成英文 id，方便调试与脚本加载。</summary>
    public static void RenameCardAssetsForDebug()
    {
        // 保持 guid 不变，只改文件名
        TryRename(CardFolder, "斩妖符.asset", "attack.asset");
        TryRename(CardFolder, "破煞符.asset", "break.asset");
    }

    static void TryRename(string folder, string from, string to)
    {
        string fromPath = folder + "/" + from;
        string toPath = folder + "/" + to;
        if (!File.Exists(fromPath)) return;
        if (File.Exists(toPath))
        {
            Debug.LogWarning($"[ApplyPlanCardData] 目标已存在，跳过重命名：{to}");
            return;
        }
        string err = AssetDatabase.RenameAsset(fromPath, Path.GetFileNameWithoutExtension(to));
        if (!string.IsNullOrEmpty(err))
            Debug.LogError("[ApplyPlanCardData] 重命名失败 " + from + " → " + to + " : " + err);
        else
            Debug.Log("[ApplyPlanCardData] 重命名 " + from + " → " + to);
    }

    public static void BindAllCardArtAndNames()
    {
        // 基础牌
        ConfigureCard(LoadOrCreate("attack", "斩妖符"), "斩妖符", LoadSprite("斩妖符图案"), 1, CardType.Attack,
            "造成6点伤害；命中红破绽可QTE", 6, 0, CardSpecialEffect.None, 0, WeaknessType.RedAttack);
        ConfigureCard(LoadOrCreate("defense", "护身符"), "护身符", LoadSprite("护身符卡牌"), 1, CardType.Defense,
            "获得5点护甲", 5, 0, CardSpecialEffect.None, 0, WeaknessType.None);
        ConfigureCard(LoadOrCreate("break", "破煞符"), "破煞符", LoadSprite("破煞符卡牌"), 1, CardType.ArmorBreak,
            "造成4点伤害并破甲；命中黄裂纹可QTE", 4, 4, CardSpecialEffect.None, 0, WeaknessType.YellowArmor);
        ConfigureCard(LoadOrCreate("hp", "聚气诀"), "聚气诀", LoadSprite("聚气符卡牌"), 0, CardType.Ability,
            "回复1点灵气", 1, 0, CardSpecialEffect.None, 0, WeaknessType.None);
        ConfigureCard(LoadOrCreate("seal", "镇魂符"), "镇魂符", LoadSprite("镇魂符卡牌"), 2, CardType.Seal,
            "造成6点伤害；命中紫封印QTE可打断蓄力", 6, 0, CardSpecialEffect.None, 0, WeaknessType.PurpleSeal);
        ConfigureCard(LoadOrCreate("fire", "烈火符"), "烈火符", LoadSprite("烈火符卡牌"), 1, CardType.Fire,
            "造成4点伤害，附加1层灼烧", 4, 0, CardSpecialEffect.ApplyBurn, 1, WeaknessType.None);

        // 奖励牌
        ConfigureCard(LoadOrCreate("reward_lianzhan", "连斩符"), "连斩符", LoadSprite("连斩符卡牌"), 1, CardType.Attack,
            "造成5点伤害；命中红破绽可QTE", 5, 0, CardSpecialEffect.None, 0, WeaknessType.RedAttack);
        ConfigureCard(LoadOrCreate("reward_lianhuo", "炼火符"), "炼火符", LoadSprite("炼火符卡牌"), 1, CardType.Fire,
            "造成3点伤害，附加2层灼烧", 3, 0, CardSpecialEffect.ApplyBurn, 2, WeaknessType.None);
        ConfigureCard(LoadOrCreate("reward_zhenhunling", "镇魂铃"), "镇魂铃", LoadSprite("镇魂铃卡牌"), 1, CardType.Seal,
            "造成3点伤害；命中紫点可打断", 3, 0, CardSpecialEffect.None, 0, WeaknessType.PurpleSeal);
        ConfigureCard(LoadOrCreate("reward_pozhen", "破阵斩"), "破阵斩", LoadSprite("破斩符卡牌"), 2, CardType.Attack,
            "造成10点伤害", 10, 0, CardSpecialEffect.None, 0, WeaknessType.RedAttack);
        ConfigureCard(LoadOrCreate("reward_yinhuo", "引火诀"), "引火诀", LoadSprite("引火符卡牌"), 1, CardType.Fire,
            "引爆敌人身上的灼烧；每层造成3点伤害，然后清除灼烧", 0, 0, CardSpecialEffect.DetonateBurn, 3, WeaknessType.None);
        ConfigureCard(LoadOrCreate("reward_dinghun", "定魂符"), "定魂符", LoadSprite("定魂符卡牌"), 2, CardType.Seal,
            "重伤并打断蓄力", 8, 0, CardSpecialEffect.None, 0, WeaknessType.PurpleSeal);

        // 测试废牌若还在，改名避免误用
        var ring = LoadCard("ring");
        if (ring != null)
        {
            ring.cardName = "测试牌_勿用";
            EditorUtility.SetDirty(ring);
        }
    }

    public static void RebuildRewardHudPanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("[ApplyPlanCardData] 场景无 Canvas，跳过奖励 UI。");
            return;
        }

        var old = canvas.transform.Find("HUD_Reward");
        RewardSelectUI oldUi = old != null ? old.GetComponent<RewardSelectUI>() : null;

        // 清旧按钮子物体，重建容器尺寸
        Transform choices = null;
        GameObject rootGo;
        RewardSelectUI ui;

        if (old != null)
        {
            rootGo = old.gameObject;
            ui = oldUi != null ? oldUi : rootGo.AddComponent<RewardSelectUI>();
            var panel = old.Find("Panel");
            if (panel != null)
            {
                var prt = panel.GetComponent<RectTransform>();
                if (prt != null) prt.sizeDelta = new Vector2(780, 420);
                choices = panel.Find("Choices");
            }
            if (choices == null)
            {
                // 兼容旧结构：找 buttonContainer
                choices = ui.buttonContainer;
            }
        }
        else
        {
            rootGo = new GameObject("HUD_Reward", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootGo.layer = 5;
            rootGo.transform.SetParent(canvas.transform, false);
            var rrt = rootGo.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;
            rootGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.layer = 5;
            panel.transform.SetParent(rootGo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(780, 420);
            panel.GetComponent<Image>().color = new Color(0.1f, 0.09f, 0.12f, 0.96f);

            var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            title.layer = 5;
            title.transform.SetParent(panel.transform, false);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -16);
            trt.sizeDelta = new Vector2(-40, 40);
            var ttmp = title.GetComponent<TextMeshProUGUI>();
            var font = TmpChineseFontUtil.FindChineseFont();
            if (font) ttmp.font = font;
            ttmp.fontSize = 28;
            ttmp.alignment = TextAlignmentOptions.Center;
            ttmp.color = Color.white;
            ttmp.text = "选择一张奖励符咒";

            var row = new GameObject("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.layer = 5;
            row.transform.SetParent(panel.transform, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.42f);
            rowRt.anchorMax = new Vector2(0.5f, 0.42f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = new Vector2(720, 340);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 24;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;

            ui = rootGo.AddComponent<RewardSelectUI>();
            ui.root = rootGo;
            ui.titleText = ttmp;
            ui.buttonContainer = row.transform;
            choices = row.transform;
        }

        // 清旧 choice 按钮，让 RewardSelectUI 按新样式重建
        if (choices != null)
        {
            for (int i = choices.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(choices.GetChild(i).gameObject);
        }

        ui.root = rootGo;
        if (ui.buttonContainer == null && choices != null)
            ui.buttonContainer = choices;
        ui.choiceButtons = new List<Button>();
        ui.choiceLabels = new List<TextMeshProUGUI>();
        ui.choiceCardImages = new List<Image>();
        ui.choiceNameLabels = new List<TextMeshProUGUI>();

        // 预建 3 个（通过反射调用 private CreateChoiceButton 不方便，Show 时会 Ensure）
        // 调大 Choices 区域
        if (choices != null)
        {
            var rowRt = choices.GetComponent<RectTransform>();
            if (rowRt != null)
            {
                rowRt.anchorMin = new Vector2(0.5f, 0.42f);
                rowRt.anchorMax = new Vector2(0.5f, 0.42f);
                rowRt.sizeDelta = new Vector2(720, 340);
            }
            var h = choices.GetComponent<HorizontalLayoutGroup>();
            if (h != null)
            {
                h.spacing = 24;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = false;
                h.childControlHeight = false;
            }
        }

        var panelTf = rootGo.transform.Find("Panel");
        if (panelTf != null)
        {
            var prt = panelTf.GetComponent<RectTransform>();
            if (prt != null) prt.sizeDelta = new Vector2(780, 420);
            var title = panelTf.Find("Title");
            if (title != null)
            {
                var ttmp = title.GetComponent<TextMeshProUGUI>();
                if (ttmp != null) ui.titleText = ttmp;
            }
        }

        rootGo.SetActive(false);

        var flow = Object.FindAnyObjectByType<BattleFlowManager>();
        if (flow != null)
        {
            flow.rewardUI = ui;
            EditorUtility.SetDirty(flow);
        }

        EditorUtility.SetDirty(rootGo);
        Debug.Log("[ApplyPlanCardData] 奖励 UI 已重建（卡面模式）。");
    }

    private static string BuildOrderPreview()
    {
        return "小妖: 斩斩护聚 | 斩烈火 | 护斩\n" +
               "石灵: 破斩护聚 | 斩烈火 | 护斩 | [奖1]斩…\n" +
               "山鬼: 镇斩护 | [奖1] | 破斩 | [奖2] | 聚 | 烈火护";
    }

    private static CardDataSO LoadCard(params string[] fileNames)
    {
        for (int i = 0; i < fileNames.Length; i++)
        {
            var c = AssetDatabase.LoadAssetAtPath<CardDataSO>($"{CardFolder}/{fileNames[i]}.asset");
            if (c != null) return c;
        }
        // 扫目录
        var guids = AssetDatabase.FindAssets("t:CardDataSO", new[] { CardFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var c = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
            if (c == null) continue;
            for (int n = 0; n < fileNames.Length; n++)
            {
                if (c.cardName == fileNames[n] || Path.GetFileNameWithoutExtension(path) == fileNames[n])
                    return c;
            }
        }
        return null;
    }

    private static CardDataSO LoadOrCreate(string fileName, string displayName)
    {
        var c = LoadCard(fileName, displayName);
        if (c != null) return c;
        c = ScriptableObject.CreateInstance<CardDataSO>();
        AssetDatabase.CreateAsset(c, $"{CardFolder}/{fileName}.asset");
        return c;
    }

    private static Sprite LoadSprite(string fileNameNoExt)
    {
        string path = $"{ArtFolder}/{fileNameNoExt}.png";
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        // 有时导入为 Texture
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            // 强制 sprite 类型
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        if (sp == null)
            Debug.LogWarning("[ApplyPlanCardData] 找不到卡图: " + path);
        return sp;
    }

    private static void ConfigureCard(CardDataSO card, string name, Sprite image, int cost, CardType type, string description,
        int effectValue, int effectValue2, CardSpecialEffect specialEffect, int specialEffectValue, WeaknessType weakness)
    {
        if (card == null) return;
        card.cardName = name;
        if (image != null) card.cardImage = image;
        card.cost = cost;
        card.cardType = type;
        card.description = description;
        card.effectValue = effectValue;
        card.effectValue2 = effectValue2;
        card.specialEffect = specialEffect;
        card.specialEffectValue = specialEffectValue;
        card.weaknessTag = weakness;
        EditorUtility.SetDirty(card);

        // 资产对象名也改成中文名，Project 窗口更好认
        string path = AssetDatabase.GetAssetPath(card);
        if (!string.IsNullOrEmpty(path))
        {
            // 保持文件名为英文 id；仅改 m_Name 显示（ScriptableObject.name）
            if (card.name != name)
            {
                // 不 Rename 文件，只改内部 name 容易和文件名不一致；用 SetDirty 即可
            }
        }
    }

    private static void SetFuXia(string fileName, string displayName, List<CardDataSO> cards)
    {
        var fuxia = AssetDatabase.LoadAssetAtPath<FuXiaOrderSO>($"{LibraryFolder}/{fileName}.asset");
        if (fuxia == null)
        {
            fuxia = ScriptableObject.CreateInstance<FuXiaOrderSO>();
            AssetDatabase.CreateAsset(fuxia, $"{LibraryFolder}/{fileName}.asset");
        }
        fuxia.displayName = displayName;
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
