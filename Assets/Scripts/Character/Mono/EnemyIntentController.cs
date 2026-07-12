using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人意图循环 + 按意图显示对应弱点。
/// 三关（小妖 / 石灵 / 山鬼）共用同一套规则：
/// - 意图与弱点由各关 <see cref="BattleStageSO.intentLoop"/> 配置；
/// - 每玩家回合弱点最多触发 1 次 QTE，触发后当回合消失。
/// Boss 蓄力为两步：Charge（紫）→ Heavy（无弱点，若未打断则结算重击）。
/// </summary>
public class EnemyIntentController : MonoBehaviour
{
    [Serializable]
    public class IntentStep
    {
        public EnemyIntentKind kind = EnemyIntentKind.Attack;
        public string displayName = "普通攻击";
        [Tooltip("本回合暴露的弱点；None = 本回合无弱点（不可触发弱点 QTE）")]
        public WeaknessType exposedWeakness = WeaknessType.None;
        [Tooltip("攻击 / 重击结算伤害")]
        public int power = 5;
        [Tooltip("防御时获得的护甲")]
        public int armorGain = 0;

        public IntentStep Clone()
        {
            return new IntentStep
            {
                kind = kind,
                displayName = displayName,
                exposedWeakness = exposedWeakness,
                power = power,
                armorGain = armorGain
            };
        }
    }

    [Header("意图表（按顺序循环）")]
    public List<IntentStep> intentLoop = new List<IntentStep>();

    [Header("弱点引用（可空，自动在子物体查找）")]
    public List<WeaknessPoint> weaknessPoints = new List<WeaknessPoint>();

    [Header("所属属性")]
    public CharacterStats stats;

    public int CurrentStepIndex { get; private set; }
    public IntentStep CurrentStep { get; private set; }
    public string CurrentDisplayName => CurrentStep != null ? CurrentStep.displayName : "—";

    /// <summary>
    /// 当前意图配置的弱点类型（不含「本回合已击破」状态）。
    /// </summary>
    public WeaknessType PlannedWeakness =>
        CurrentStep != null ? CurrentStep.exposedWeakness : WeaknessType.None;

    /// <summary>
    /// 实际可命中的弱点：本回合已触发过 QTE 后视为 None（弱点消失）。
    /// </summary>
    public WeaknessType CurrentWeakness =>
        WeaknessExpendedThisTurn ? WeaknessType.None : PlannedWeakness;

    /// <summary>本回合是否已用掉弱点（命中并触发 QTE 后为 true）</summary>
    public bool WeaknessExpendedThisTurn { get; private set; }

    /// <summary>当前是否还能对弱点触发 QTE</summary>
    public bool CanTriggerWeaknessQte =>
        !WeaknessExpendedThisTurn && PlannedWeakness != WeaknessType.None;

    /// <summary>镇魂 QTE 成功后打断蓄力（在蓄力回合内有效）</summary>
    public bool ChargeInterrupted { get; private set; }

    /// <summary>蓄力蓄势已完成且未被打断，等待重击释放</summary>
    public bool IsCharging { get; private set; }

    public event Action OnIntentChanged;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<CharacterStats>();
        if (stats == null)
            stats = GetComponentInParent<CharacterStats>();
        RefreshWeaknessList();
        EnsureDefaultLoop();
    }

    private void EnsureDefaultLoop()
    {
        if (intentLoop != null && intentLoop.Count > 0) return;
        // 与策划案「小妖」教学节奏类似的回退表
        intentLoop = new List<IntentStep>
        {
            new IntentStep
            {
                kind = EnemyIntentKind.Attack,
                displayName = "普通攻击",
                exposedWeakness = WeaknessType.RedAttack,
                power = 5
            },
            new IntentStep
            {
                kind = EnemyIntentKind.Attack,
                displayName = "普通攻击",
                exposedWeakness = WeaknessType.None,
                power = 5
            },
            new IntentStep
            {
                kind = EnemyIntentKind.Attack,
                displayName = "攻击",
                exposedWeakness = WeaknessType.RedAttack,
                power = 7
            }
        };
    }

    /// <summary>
    /// 从关卡数据深拷贝意图表，避免与 SO 共享引用、并保证每步字段完整。
    /// </summary>
    public void SetIntentLoop(IList<IntentStep> source, bool resetToFirst = true)
    {
        intentLoop = new List<IntentStep>();
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null) continue;
                intentLoop.Add(source[i].Clone());
            }
        }

        EnsureDefaultLoop();

        if (resetToFirst)
        {
            ResetIntent();
        }
        else
        {
            ApplyCurrentStep();
        }
    }

    public void ResetIntent()
    {
        CurrentStepIndex = 0;
        ChargeInterrupted = false;
        IsCharging = false;
        WeaknessExpendedThisTurn = false;
        ApplyCurrentStep();
    }

    /// <summary>
    /// 玩家回合开始时：展示本回合意图与弱点（尚未执行）
    /// </summary>
    public void PresentIntentForPlayerTurn()
    {
        // 新一回合展示时清掉「本回合刚打断」的瞬时标记；
        // IsCharging 需跨回合保留（蓄力 → 下一回合释放重击）。
        ChargeInterrupted = false;
        // 每玩家回合刷新：恢复本回合可击破一次的弱点额度
        WeaknessExpendedThisTurn = false;
        // 换模 / AR 恢复后可能丢引用，每次展示前重扫弱点
        RefreshWeaknessList();
        ApplyCurrentStep();
    }

    /// <summary>
    /// 命中弱点并触发 QTE 后调用：本回合弱点立即消失，不可再次弱点 QTE。
    /// 对当前挂载此组件的任意怪物生效（不限关卡）。
    /// </summary>
    public void ConsumeWeaknessThisTurn()
    {
        if (WeaknessExpendedThisTurn) return;
        WeaknessExpendedThisTurn = true;
        // 确保扫到的是本怪物身上的弱点点（换关后 stats / 子树可能变过）
        if (stats == null)
        {
            stats = GetComponent<CharacterStats>();
            if (stats == null)
                stats = GetComponentInParent<CharacterStats>();
        }
        RefreshWeaknessVisibility();
        OnIntentChanged?.Invoke();
        Debug.Log($"[EnemyIntent] [{name}] 本回合弱点已击破，关闭弱点显示（本回合不可再 QTE）。");
    }

    public void InterruptCharge()
    {
        // 仅在「正在蓄力」意图下可打断；打断后取消已挂起的重击
        if (CurrentStep != null && CurrentStep.kind == EnemyIntentKind.Charge)
        {
            ChargeInterrupted = true;
            IsCharging = false;
            Debug.Log("[EnemyIntent] 蓄力被打断！");
            OnIntentChanged?.Invoke();
        }
    }

    /// <summary>
    /// 敌人回合执行当前意图，然后推进到下一步并立刻刷新弱点显示。
    /// </summary>
    public void ExecuteAndAdvance(CharacterStats player)
    {
        if (CurrentStep == null)
            ApplyCurrentStep();

        var step = CurrentStep;
        int executedIndex = CurrentStepIndex;

        if (step != null && stats != null && stats.CurrentHP > 0)
        {
            switch (step.kind)
            {
                case EnemyIntentKind.Attack:
                    if (player != null)
                    {
                        player.TakeDamage(step.power);
                        Debug.Log($"[EnemyIntent] {step.displayName}，造成 {step.power} 点伤害");
                    }
                    break;

                case EnemyIntentKind.Defend:
                    stats.AddArmor(step.armorGain);
                    Debug.Log($"[EnemyIntent] 获得 {step.armorGain} 点护甲");
                    break;

                case EnemyIntentKind.Charge:
                    // 蓄势：本步不造成伤害。打断则不进入 IsCharging。
                    if (ChargeInterrupted)
                    {
                        IsCharging = false;
                        Debug.Log("[EnemyIntent] 蓄力被打断，取消重击挂起。");
                    }
                    else
                    {
                        IsCharging = true;
                        Debug.Log("[EnemyIntent] 蓄力完成，下回合将释放重击。");
                    }
                    break;

                case EnemyIntentKind.Heavy:
                    if (IsCharging && !ChargeInterrupted)
                    {
                        if (player != null)
                        {
                            player.TakeDamage(step.power);
                            Debug.Log($"[EnemyIntent] 蓄力重击！造成 {step.power} 点伤害");
                        }
                    }
                    else
                    {
                        Debug.Log("[EnemyIntent] 重击未释放（蓄力已被打断或未蓄力）。");
                    }
                    IsCharging = false;
                    break;
            }
        }
        else if (step == null)
        {
            Debug.LogWarning("[EnemyIntent] ExecuteAndAdvance：CurrentStep 为空，无法结算。");
        }

        // 推进到下一步，并立刻应用弱点（避免卡在上一步红弱点）
        int count = intentLoop != null ? intentLoop.Count : 0;
        if (count <= 0)
        {
            EnsureDefaultLoop();
            count = intentLoop.Count;
        }

        CurrentStepIndex = (CurrentStepIndex + 1) % Mathf.Max(1, count);
        ChargeInterrupted = false;

        RefreshWeaknessList();
        ApplyCurrentStep();

        Debug.Log($"[EnemyIntent] 推进 {executedIndex} → {CurrentStepIndex}/{count}，下一意图：{CurrentDisplayName}，弱点：{CurrentWeakness}");
    }

    private void ApplyCurrentStep()
    {
        EnsureDefaultLoop();
        if (intentLoop == null || intentLoop.Count == 0)
        {
            CurrentStep = null;
            RefreshWeaknessVisibility();
            return;
        }

        CurrentStepIndex = Mathf.Clamp(CurrentStepIndex, 0, intentLoop.Count - 1);
        CurrentStep = intentLoop[CurrentStepIndex];
        RefreshWeaknessVisibility();
        OnIntentChanged?.Invoke();
        Debug.Log($"[EnemyIntent] 当前步骤[{CurrentStepIndex}]：{CurrentStep.displayName}，暴露弱点：{CurrentStep.exposedWeakness}");
    }

    public void RefreshWeaknessVisibility()
    {
        // 已击破 / 配置无弱点 → 全部关闭；否则只亮 PlannedWeakness 对应色
        var activeType = WeaknessExpendedThisTurn ? WeaknessType.None : PlannedWeakness;

        // 每次以场景里实际弱点为准，避免列表空/过期导致永远关不掉红点
        RefreshWeaknessList();

        for (int i = 0; i < weaknessPoints.Count; i++)
        {
            var wp = weaknessPoints[i];
            if (wp == null) continue;
            bool on = activeType != WeaknessType.None && wp.weaknessType == activeType;
            wp.SetActiveWeakness(on);
        }

        Debug.Log($"[EnemyIntent] 弱点刷新：planned={PlannedWeakness} active={activeType} expended={WeaknessExpendedThisTurn} 点数={weaknessPoints.Count}");
    }

    public void RefreshWeaknessList()
    {
        weaknessPoints = new List<WeaknessPoint>();
        var seen = new HashSet<WeaknessPoint>();

        void AddRange(WeaknessPoint[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var wp = arr[i];
                if (wp == null || seen.Contains(wp)) continue;
                // 排除已废弃的场景直属 Weakness_*（Ellen 旧节点）
                if (wp.transform.parent == transform && wp.name.StartsWith("Weakness_")
                    && !wp.gameObject.activeSelf && wp.transform.Find("WeaknessVisual") == null)
                {
                    // 仍允许加入：意图系统会负责 SetActive；旧节点由 AnchorSetup 关掉
                }
                seen.Add(wp);
                weaknessPoints.Add(wp);
            }
        }

        // 1) ActiveMonster（旧换模路径）
        Transform activeMonster = transform.Find("ActiveMonster");
        if (activeMonster != null)
            AddRange(activeMonster.GetComponentsInChildren<WeaknessPoint>(true));

        // 2) 自身子树（当前三关：弱点直接挂在怪物 Prefab 头骨下）
        AddRange(GetComponentsInChildren<WeaknessPoint>(true));

        // 3) 若 Intent 挂在父节点，stats 在子物体上，再扫一层
        if (stats != null && stats.transform != transform)
            AddRange(stats.GetComponentsInChildren<WeaknessPoint>(true));

        // 4) 父级 CharacterStats（Intent 挂在子物体时）
        if (stats == null)
        {
            stats = GetComponent<CharacterStats>();
            if (stats == null)
                stats = GetComponentInParent<CharacterStats>();
        }
        if (stats != null && stats.transform != transform)
            AddRange(stats.GetComponentsInChildren<WeaknessPoint>(true));
    }
}
