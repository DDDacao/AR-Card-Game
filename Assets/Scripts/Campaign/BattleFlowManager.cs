using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战役流程管理器（解耦版）：通过 Vuforia 扫描对应 ImageTarget 卡片来触发对应的关卡对战，支持线性关卡解锁与丢失识别挂起。
/// </summary>
public class BattleFlowManager : MonoBehaviour
{
    public static BattleFlowManager Instance { get; private set; }

    [Header("战役配置")]
    public CampaignSO campaign;

    [Header("引用")]
    public TurnManager turnManager;
    public CardDeck cardDeck;
    public BattleResultUI resultUI;
    public RewardSelectUI rewardUI;
    public BattleInfoUI battleInfoUI;

    [Header("编辑器调试")]
    [Tooltip("在编辑器中开启 AR 模拟控制台，便于调试")]
    public bool skipARForEditor = true;

    [Header("关卡怪物模型（旧版 Prefab 引用，保留防报错）")]
    public GameObject xiaoYaoMonsterPrefab;
    public GameObject shiLingMonsterPrefab;
    public GameObject shanGuiMonsterPrefab;

    [Header("本局获得的奖励符（带入后续符匣）")]
    public List<CardDataSO> earnedRewards = new List<CardDataSO>();

    [Header("进度管理")]
    [SerializeField] private int currentUnlockedStageIndex = 0;

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

        // 仅当场景中没有 BattleBootstrap 时，才在 Start 中自动启动战役（防抢跑并确保正确的相机初始化顺序）
        if (FindAnyObjectByType<BattleBootstrap>() == null && campaign != null)
        {
            StartCampaign();
        }
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

    /// <summary>
    /// 控制怪物模型可见性，包括其下所有 Renderer、Collider 和 Canvas。
    /// 会跳过弱点节点（WeaknessPoint）——弱点显隐由 EnemyIntentController 按回合表管理。
    /// </summary>
    private void SetMonsterModelVisibility(GameObject monsterGo, bool visible)
    {
        if (monsterGo == null) return;

        monsterGo.SetActive(visible);

        var renderers = monsterGo.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (r.GetComponentInParent<WeaknessPoint>() != null) continue;
            r.enabled = visible;
        }

        var colliders = monsterGo.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (c == null) continue;
            if (c.GetComponentInParent<WeaknessPoint>() != null) continue;
            c.enabled = visible;
        }

        var canvases = monsterGo.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            canvas.enabled = visible;
        }

        // AR 恢复显示后，按当前意图重新同步弱点（避免三色常亮 / 红弱点不退）
        if (visible)
        {
            var intent = monsterGo.GetComponent<EnemyIntentController>();
            if (intent == null)
                intent = monsterGo.GetComponentInParent<EnemyIntentController>();
            if (intent == null && turnManager != null)
                intent = turnManager.enemyIntent;
            if (intent != null)
                intent.RefreshWeaknessVisibility();
        }
    }

    /// <summary>
    /// 初始化战役，重置进度并进入扫卡等待阶段
    /// </summary>
    public void StartCampaign()
    {
        ResolveRefs();
        Subscribe();
        CampaignCompleted = false;
        earnedRewards.Clear();
        currentUnlockedStageIndex = 0;
        CurrentStageIndex = 0;

        // 彻底关闭并隐藏旧的场景 Ellen_skin (2)，避免组件冲突和遮挡
        GameObject ellen = GameObject.Find("Ellen_skin (2)");
        if (ellen != null)
        {
            ellen.SetActive(false);
            Debug.Log("[BattleFlow] 已将旧版 Ellen_skin (2) 禁用。");
        }

        // 隐藏场景中所有的 ImageTarget 怪物，等待扫描触发
        for (int i = 0; i < 3; i++)
        {
            var m = FindMonsterInScene(i);
            if (m != null)
            {
                SetMonsterModelVisibility(m.gameObject, false);
            }
        }

        if (campaign == null || campaign.stages == null || campaign.stages.Count == 0)
        {
            Debug.LogWarning("[BattleFlow] 无战役配置，直接启动战斗。");
            if (turnManager != null) turnManager.StartBattle();
            return;
        }

#if UNITY_EDITOR
        if (skipARForEditor)
        {
            Debug.Log("[BattleFlow] Editor AR 模拟调试：自动触发第 1 关。");
            var monster = FindMonsterInScene(0);
            if (monster != null)
            {
                OnARCardTracked(0, monster);
                return;
            }
        }
#endif

        // 真机或非模拟模式：等待扫描第一张卡牌
        if (resultUI != null)
        {
            resultUI.ShowMessage("请扫描第 1 关物理卡牌以开启对战！", false, "", null);
        }
    }

    public void RetryCurrentStage()
    {
        var monster = FindMonsterInScene(CurrentStageIndex);
        if (monster != null)
        {
            StartStageBattle(CurrentStageIndex, monster);
        }
        else
        {
            Debug.LogError($"[BattleFlow] 无法重试：在场景中找不到关卡 {CurrentStageIndex + 1} 的怪物。");
            turnManager?.RestartBattle();
        }
    }

    /// <summary>
    /// 核心方法：开始指定关卡对战，并注入对应的怪物实例
    /// </summary>
    public void StartStageBattle(int index, CharacterStats monster)
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

        // 1. 确保怪物实例挂载并初始化了相应组件
        if (monster != null)
        {
            // 确保显示出来
            SetMonsterModelVisibility(monster.gameObject, true);

            if (monster.templateData == null)
                monster.templateData = CurrentStage.enemyData;
            
            monster.InitializeStats();

            // 动画桥梁初始化
            var animBridge = monster.GetComponent<MonsterAnimationBridge>();
            if (animBridge == null)
                animBridge = monster.gameObject.AddComponent<MonsterAnimationBridge>();
            
            var animator = monster.GetComponent<Animator>();
            animBridge.BindTargetAnimator(animator, monster.gameObject.name);
            animBridge.PlayIdle();

            // 弱点自动锚定挂接
            WeaknessAnchorSetup.ApplyForStage(monster.gameObject, monster.gameObject, index);
        }

        // 2. 注入敌人目标到 TurnManager
        turnManager.SetEnemyTarget(monster);

        // 确保 URP 双相机 Stack 正常渲染手牌（防 Vuforia 启动/换模时清空 Overlay 相机堆叠）
        if (CardCameraManager.Instance != null)
        {
            CardCameraManager.Instance.SetupCameraStack();
        }

        // 3. 敌人显示名更新
        if (battleInfoUI != null)
            battleInfoUI.enemyDisplayName = CurrentStage.enemyDisplayName;

        // 4. 符匣初始化（带入奖励）
        if (cardDeck != null)
        {
            cardDeck.useFixedOrder = true;
            cardDeck.fuXiaOrder = CurrentStage.fuXiaOrder;
            cardDeck.runtimePrefixCards = new List<CardDataSO>();
            cardDeck.runtimeEarnedRewards = new List<CardDataSO>(earnedRewards);
            cardDeck.runtimeRewardInsertions = CurrentStage.rewardInsertions != null
                ? new List<BattleStageSO.RewardCardInsertion>(CurrentStage.rewardInsertions)
                : new List<BattleStageSO.RewardCardInsertion>();
            
            cardDeck.ClearAllForBattleReset();
        }

        // 5. 敌人意图初始化（深拷贝策划案回合表，并立刻按第 1 步刷新弱点）
        if (turnManager.enemyIntent != null)
        {
            if (CurrentStage.intentLoop != null && CurrentStage.intentLoop.Count > 0)
            {
                turnManager.enemyIntent.SetIntentLoop(CurrentStage.intentLoop, resetToFirst: true);
            }
            else
            {
                turnManager.enemyIntent.RefreshWeaknessList();
                turnManager.enemyIntent.ResetIntent();
            }
        }

        Debug.Log($"[BattleFlow] 成功开启战斗！关卡 {index + 1}/{campaign.stages.Count}：{CurrentStage.stageName}");
        
        // 隐藏开始对齐或锁定提示
        if (resultUI != null)
            resultUI.HidePanel();

        // 恢复手牌拖动
        CardDragHandler.InteractionEnabled = true;

        if (index == 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM();
        }

        turnManager.StartBattle();
    }

    /// <summary>
    /// 当 ARImageTarget 被扫描识别时触发
    /// </summary>
    public void OnARCardTracked(int stageIndex, CharacterStats monster)
    {
        ResolveRefs();

        // 1. 如果战斗中且扫描的是当前关卡，视为丢失识别后的恢复
        if (turnManager.IsBattleActive && !turnManager.BattleEnded)
        {
            if (stageIndex == CurrentStageIndex)
            {
                if (monster != null)
                {
                    SetMonsterModelVisibility(monster.gameObject, true);
                }
                if (resultUI != null)
                    resultUI.HidePanel();
                CardDragHandler.InteractionEnabled = true;
                Debug.Log($"[AR] 当前关卡 {stageIndex + 1} 的卡牌重新获得识别，恢复战斗。");
            }
            return;
        }

        // 2. 如果正在选择三选一奖励，忽略其他卡牌扫描
        if (rewardUI != null && rewardUI.gameObject.activeInHierarchy)
        {
            Debug.Log($"[AR] 正在选择卡牌奖励中，忽略扫描。");
            return;
        }

        // 3. 校验线性进度解锁
        if (stageIndex != currentUnlockedStageIndex)
        {
            ShowStageLockWarning(stageIndex);
            return;
        }

        // 4. 未在战斗中，则进入战斗
        StartStageBattle(stageIndex, monster);
    }

    /// <summary>
    /// 当 ARImageTarget 丢失识别时触发
    /// </summary>
    public void OnARCardLost(int stageIndex)
    {
        // 只有当前正在挑战的关卡丢失了识别，才挂起出牌
        if (turnManager.IsBattleActive && !turnManager.BattleEnded && stageIndex == CurrentStageIndex)
        {
            Debug.LogWarning($"[AR] 当前挑战关卡 {stageIndex + 1} 卡牌丢失识别，挂起战斗表现。");
            if (resultUI != null)
            {
                resultUI.ShowMessage("AR 识别丢失！\n请重新对准卡牌以继续战斗...", false, "", null);
            }
            CardDragHandler.InteractionEnabled = false;

            // 隐藏怪物模型
            var monster = FindMonsterInScene(stageIndex);
            if (monster != null)
            {
                SetMonsterModelVisibility(monster.gameObject, false);
            }
        }
    }

    private void ShowStageLockWarning(int scannedIndex)
    {
        if (resultUI == null) return;

        if (scannedIndex > currentUnlockedStageIndex)
        {
            resultUI.ShowMessage($"关卡未解锁！\n请先扫描第 {currentUnlockedStageIndex + 1} 关卡牌挑战妖怪。", true, "确定", () =>
            {
                resultUI.ShowMessage($"请扫描第 {currentUnlockedStageIndex + 1} 关卡牌以开启对战！", false, "", null);
            });
        }
        else if (scannedIndex < currentUnlockedStageIndex)
        {
            resultUI.ShowMessage("该妖怪已被成功封印！", true, "确定", () =>
            {
                resultUI.ShowMessage($"请扫描第 {currentUnlockedStageIndex + 1} 关卡牌以开启对战！", false, "", null);
            });
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
        Debug.Log($"[BattleFlow] 关卡胜利：{CurrentStage?.stageName}");

        if (CurrentStage != null && CurrentStage.HasRewards)
        {
            if (resultUI != null)
                resultUI.HidePanel();
            if (rewardUI != null)
            {
                if (rewardUI.root != null)
                    rewardUI.root.transform.SetAsLastSibling();
                rewardUI.Show(CurrentStage.rewardChoices, OnRewardPicked);
                return;
            }
            if (CurrentStage.rewardChoices.Count > 0)
                earnedRewards.Add(CurrentStage.rewardChoices[0]);
            ProceedAfterReward();
            return;
        }

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
        // 隐藏当前击败的怪物模型
        var clearedMonster = FindMonsterInScene(CurrentStageIndex);
        if (clearedMonster != null)
        {
            SetMonsterModelVisibility(clearedMonster.gameObject, false);
        }

        // 胜出且领完奖后，关卡解锁步进
        if (CurrentStageIndex == currentUnlockedStageIndex)
        {
            currentUnlockedStageIndex++;
        }

        if (campaign != null && currentUnlockedStageIndex < campaign.stages.Count)
        {
            // 回到扫卡等待状态，提示扫描下一张卡
            if (resultUI != null)
            {
                resultUI.ShowVictory(
                    $"请扫描第 {currentUnlockedStageIndex + 1} 关卡牌继续挑战",
                    false, "", null);
            }
            return;
        }

        // 全通关
        CampaignCompleted = true;
        currentUnlockedStageIndex = 0;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlayCampaignVictory();
        }

        if (resultUI != null)
        {
            resultUI.ShowVictory("三关尽破，封妖成功！", true, "再来一局", () =>
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

    private void SwapMonsterModel(int stageIndex)
    {
        // 已弃用：怪物已挂在各自的 ImageTarget 下，不再通过 Ellen 父物体动态换模
    }

    private CharacterStats FindMonsterInScene(int stageIndex)
    {
        string parentName = stageIndex switch
        {
            0 => "ImageTarget_Stage1",
            1 => "ImageTarget_Stage2",
            2 => "ImageTarget_Stage3",
            _ => ""
        };
        string childName = stageIndex switch
        {
            0 => "Vespomorph",
            1 => "Cavecrawler",
            2 => "Drackmahre",
            _ => ""
        };

        GameObject parentGo = GameObject.Find(parentName);
        GameObject monsterGo = null;

        if (parentGo != null)
        {
            Transform childTf = parentGo.transform.Find(childName);
            if (childTf != null)
            {
                monsterGo = childTf.gameObject;
            }
        }

        if (monsterGo == null)
        {
            // 兜底直接找根目录
            monsterGo = GameObject.Find(childName);
            if (monsterGo == null)
            {
                // 搜索所有包括未激活的
                var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name == childName && t.hideFlags == HideFlags.None)
                    {
                        monsterGo = t.gameObject;
                        break;
                    }
                }
            }
        }

        if (monsterGo != null)
        {
            var stats = monsterGo.GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = monsterGo.AddComponent<CharacterStats>();
                Debug.Log($"[AR Simulate] 动态为 {monsterGo.name} 挂载 CharacterStats 组件。");
            }
            return stats;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!skipARForEditor) return;

        GUILayout.BeginArea(new Rect(10, 100, 240, 240));
        GUILayout.Box("=== AR 模拟控制台 ===");

        if (GUILayout.Button("模拟扫卡：第 1 关 (小妖)"))
        {
            SimulateARScan(0);
        }
        if (GUILayout.Button("模拟扫卡：第 2 关 (石灵)"))
        {
            SimulateARScan(1);
        }
        if (GUILayout.Button("模拟扫卡：第 3 关 (山鬼)"))
        {
            SimulateARScan(2);
        }

        GUILayout.Space(5);
        if (GUILayout.Button("模拟丢失当前对战卡牌"))
        {
            OnARCardLost(CurrentStageIndex);
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"当前解锁关卡: 第 {currentUnlockedStageIndex + 1} 关");
        GUILayout.Label($"当前对战关卡: {(turnManager != null && turnManager.IsBattleActive ? "第 " + (CurrentStageIndex + 1) + " 关" : "未在对战中")}");
        GUILayout.EndArea();
    }

    private void SimulateARScan(int stageIndex)
    {
        var monster = FindMonsterInScene(stageIndex);
        if (monster != null)
        {
            OnARCardTracked(stageIndex, monster);
        }
        else
        {
            Debug.LogError($"[AR Simulate] 找不到模拟的怪物节点，请检查场景中是否有 ImageTarget 下的怪物实例。");
        }
    }
#endif
}
