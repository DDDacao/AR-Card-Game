using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人意图循环 + 按意图显示对应弱点。
/// 默认循环：攻击(红) → 防御(黄) → 蓄力(紫) → …
/// </summary>
public class EnemyIntentController : MonoBehaviour
{
    [Serializable]
    public class IntentStep
    {
        public EnemyIntentKind kind = EnemyIntentKind.Attack;
        public string displayName = "普通攻击";
        public WeaknessType exposedWeakness = WeaknessType.RedAttack;
        [Tooltip("攻击/蓄力结算伤害")]
        public int power = 5;
        [Tooltip("防御时获得的护甲")]
        public int armorGain = 0;
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
    public WeaknessType CurrentWeakness => CurrentStep != null ? CurrentStep.exposedWeakness : WeaknessType.None;

    /// <summary>镇魂 QTE 成功后打断蓄力</summary>
    public bool ChargeInterrupted { get; private set; }

    public event Action OnIntentChanged;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<CharacterStats>();
        if (weaknessPoints == null || weaknessPoints.Count == 0)
            weaknessPoints = new List<WeaknessPoint>(GetComponentsInChildren<WeaknessPoint>(true));
        EnsureDefaultLoop();
    }

    private void EnsureDefaultLoop()
    {
        if (intentLoop != null && intentLoop.Count > 0) return;
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
                kind = EnemyIntentKind.Defend,
                displayName = "正在防御",
                exposedWeakness = WeaknessType.YellowArmor,
                power = 0,
                armorGain = 6
            },
            new IntentStep
            {
                kind = EnemyIntentKind.Charge,
                displayName = "蓄力中",
                exposedWeakness = WeaknessType.PurpleSeal,
                power = 12
            }
        };
    }

    public void ResetIntent()
    {
        CurrentStepIndex = 0;
        ChargeInterrupted = false;
        ApplyCurrentStep();
    }

    /// <summary>
    /// 玩家回合开始时：展示本回合意图与弱点（尚未执行）
    /// </summary>
    public void PresentIntentForPlayerTurn()
    {
        ChargeInterrupted = false;
        ApplyCurrentStep();
    }

    public void InterruptCharge()
    {
        if (CurrentStep != null && CurrentStep.kind == EnemyIntentKind.Charge)
        {
            ChargeInterrupted = true;
            Debug.Log("[EnemyIntent] 蓄力被打断！");
        }
    }

    /// <summary>
    /// 敌人回合执行当前意图，然后推进到下一步
    /// </summary>
    public void ExecuteAndAdvance(CharacterStats player)
    {
        if (CurrentStep == null)
            ApplyCurrentStep();

        var step = CurrentStep;
        if (step != null && stats != null && stats.CurrentHP > 0)
        {
            switch (step.kind)
            {
                case EnemyIntentKind.Attack:
                    if (player != null)
                    {
                        player.TakeDamage(step.power);
                        Debug.Log($"[EnemyIntent] 普通攻击，造成 {step.power} 点伤害");
                    }
                    break;

                case EnemyIntentKind.Defend:
                    stats.AddArmor(step.armorGain);
                    Debug.Log($"[EnemyIntent] 获得 {step.armorGain} 点护甲");
                    break;

                case EnemyIntentKind.Charge:
                    if (ChargeInterrupted)
                    {
                        Debug.Log("[EnemyIntent] 蓄力被打断，本回合不释放重击。");
                    }
                    else if (player != null)
                    {
                        player.TakeDamage(step.power);
                        Debug.Log($"[EnemyIntent] 蓄力重击！造成 {step.power} 点伤害");
                    }
                    break;
            }
        }

        // 推进意图
        CurrentStepIndex = (CurrentStepIndex + 1) % Mathf.Max(1, intentLoop.Count);
        ChargeInterrupted = false;
        // 下一步意图在玩家回合开始时 Present
    }

    private void ApplyCurrentStep()
    {
        EnsureDefaultLoop();
        if (intentLoop.Count == 0)
        {
            CurrentStep = null;
            return;
        }

        CurrentStepIndex = Mathf.Clamp(CurrentStepIndex, 0, intentLoop.Count - 1);
        CurrentStep = intentLoop[CurrentStepIndex];
        RefreshWeaknessVisibility();
        OnIntentChanged?.Invoke();
        Debug.Log($"[EnemyIntent] 当前意图：{CurrentStep.displayName}，暴露弱点：{CurrentStep.exposedWeakness}");
    }

    public void RefreshWeaknessVisibility()
    {
        if (weaknessPoints == null) return;
        var activeType = CurrentWeakness;
        for (int i = 0; i < weaknessPoints.Count; i++)
        {
            var wp = weaknessPoints[i];
            if (wp == null) continue;
            bool on = activeType != WeaknessType.None && wp.weaknessType == activeType;
            wp.SetActiveWeakness(on);
        }
    }

    public void RefreshWeaknessList()
    {
        weaknessPoints = new List<WeaknessPoint>();

        // 优先使用换模后的 ActiveMonster 上的弱点（Prefab 内）
        Transform activeMonster = transform.Find("ActiveMonster");
        if (activeMonster != null)
        {
            var onMonster = activeMonster.GetComponentsInChildren<WeaknessPoint>(true);
            if (onMonster != null && onMonster.Length > 0)
            {
                weaknessPoints.AddRange(onMonster);
                return;
            }
        }

        // 回退：整棵子树（并跳过已禁用的场景旧弱点）
        var all = GetComponentsInChildren<WeaknessPoint>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            // 场景 Ellen 根下旧 Weakness_* 若被关掉仍会扫到，仅收集 activeSelf 或在 ActiveMonster 下的
            if (!all[i].gameObject.activeInHierarchy && all[i].transform.root == transform.root)
            {
                // 允许 inactive 的弱点仍进列表（意图会 SetActive），但排除被永久废弃的场景直属节点
                if (all[i].transform.parent == transform && all[i].name.StartsWith("Weakness_"))
                    continue;
            }
            weaknessPoints.Add(all[i]);
        }
    }
}
