using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    public PoolTool poolTool;
    public List<CardDataSO> cardDataList;

    public CardLibrarySO newGameCardLibrary;
    public CardLibrarySO currentLibrary;

    [Header("QTE 成功额外伤害倍率（追加 = base * 该值）")]
    public float qteBonusMultiplier = 1f;

    private void Awake()
    {
        Instance = this;

        InitializeCardDataList();

        if (currentLibrary != null)
            currentLibrary.cardLibraryList.Clear();

        if (newGameCardLibrary != null && currentLibrary != null)
        {
            foreach (var item in newGameCardLibrary.cardLibraryList)
            {
                currentLibrary.cardLibraryList.Add(item);
            }
        }
    }

    private void OnDisable()
    {
        if (currentLibrary != null)
            currentLibrary.cardLibraryList.Clear();
    }

#region 初始化卡牌数据

    private void InitializeCardDataList()
    {
        Addressables.LoadAssetsAsync<CardDataSO>("CardData", null).Completed += OnCardDataLoaded;
    }

    private void OnCardDataLoaded(AsyncOperationHandle<IList<CardDataSO>> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            cardDataList = new List<CardDataSO>(handle.Result);
        }
        else
        {
            Debug.LogError("No CardData Found!");
        }
    }
#endregion

    public GameObject GetCardObj()
    {
        var cardObj = poolTool.GetObj();
        cardObj.transform.localScale = Vector3.zero;
        return cardObj;
    }

    public void ReleaseCardObj(GameObject obj)
    {
        poolTool.ReleaseObj(obj);
    }

    public void ExecuteCard(CardDataSO card, CharacterStats player, CharacterStats enemy)
    {
        ExecuteCard(card, player, enemy, false, null);
    }

    public void ExecuteCard(CardDataSO card, CharacterStats player, CharacterStats enemy, bool hitWeakness, WeaknessPoint weakness)
    {
        if (card == null) return;

        switch (card.cardType)
        {
            case CardType.Attack:
            case CardType.ArmorBreak:
            case CardType.Seal:
            case CardType.Fire:
                ResolveTargetedCard(card, player, enemy, hitWeakness, weakness);
                return;

            case CardType.Defense:
                if (player != null)
                {
                    player.AddArmor(card.effectValue);
                    Debug.Log($"[CardManager] 使用防御牌【{card.cardName}】，主角获得 {card.effectValue} 点护甲！");
                }
                break;

            case CardType.Ability:
                if (player != null)
                {
                    int energyGain = card.effectValue > 0 ? card.effectValue : card.effectValue2;
                    if (energyGain <= 0) energyGain = 1;
                    player.AddEnergy(energyGain);
                    Debug.Log($"[CardManager] 使用技能牌【{card.cardName}】，主角回复了 {energyGain} 点灵气！");
                }
                break;
        }

        if (TurnManager.Instance != null)
            TurnManager.Instance.CheckBattleEnd();
    }

    private void ResolveTargetedCard(CardDataSO card, CharacterStats player, CharacterStats enemy, bool hitWeakness, WeaknessPoint weakness)
    {
        if (enemy == null)
        {
            Debug.LogWarning($"[CardManager] 【{card.cardName}】失败，没有目标敌人！");
            return;
        }

        // 三关怪物共用：确保当前被打的敌人身上有意图/弱点控制器
        var intent = EnsureEnemyIntent(enemy, weakness);
        // 本回合只能打一次弱点（小妖 / 石灵 / 山鬼 同一规则）
        bool weaknessStillOpen = intent != null && intent.CanTriggerWeaknessQte;

        bool canQte = hitWeakness
                      && weaknessStillOpen
                      && weakness != null
                      && card.ResolveWeaknessTag() != WeaknessType.None
                      && weakness.weaknessType == card.ResolveWeaknessTag();

        if (canQte)
        {
            var weaknessType = weakness.weaknessType;
            Debug.Log($"[CardManager] 命中弱点【{weaknessType}】，触发 QTE！牌：{card.cardName} 敌人：{enemy.name}");

            // 触发 QTE 当即关闭本回合弱点（无论 QTE 成败，本回合不可再打弱点）
            intent.ConsumeWeaknessThisTurn();

            if (QTEManager.Instance != null)
            {
                // 成功回调在 QTE 结果界面关闭后触发（跳字/闪色对齐）
                QTEManager.Instance.StartClickQTE(success =>
                {
                    ApplyTargetedEffect(card, enemy, success, weaknessType);
                    if (TurnManager.Instance != null)
                        TurnManager.Instance.CheckBattleEnd();
                });
            }
            else
            {
                // 无 QTE 系统时视为直接成功，仍占用本回合弱点额度
                ApplyTargetedEffect(card, enemy, true, weaknessType);
                if (TurnManager.Instance != null)
                    TurnManager.Instance.CheckBattleEnd();
            }
        }
        else
        {
            if (hitWeakness && intent != null && intent.WeaknessExpendedThisTurn)
                Debug.Log($"[CardManager] 【{card.cardName}】本回合弱点已用过（{enemy.name}），按普通命中结算。");

            var tag = hitWeakness && weaknessStillOpen && weakness != null
                ? weakness.weaknessType
                : WeaknessType.None;
            ApplyTargetedEffect(card, enemy, false, tag);
            if (TurnManager.Instance != null)
                TurnManager.Instance.CheckBattleEnd();
        }
    }

    /// <summary>
    /// 解析/挂载当前敌人的意图控制器（三关各自怪物实例都适用）。
    /// </summary>
    private static EnemyIntentController EnsureEnemyIntent(CharacterStats enemy, WeaknessPoint weakness)
    {
        if (enemy == null) return null;

        EnemyIntentController intent = null;

        // 1) 优先用 TurnManager 上当前对战敌人（开战时已注入）
        if (TurnManager.Instance != null && TurnManager.Instance.enemyStats == enemy)
        {
            intent = TurnManager.Instance.enemyIntent;
            if (intent == null)
            {
                TurnManager.Instance.SetEnemyTarget(enemy);
                intent = TurnManager.Instance.enemyIntent;
            }
        }

        // 2) 从被命中的弱点向上找
        if (intent == null && weakness != null)
        {
            intent = weakness.GetComponentInParent<EnemyIntentController>();
            var owner = weakness.GetOwner();
            if (intent == null && owner != null)
            {
                intent = owner.GetComponent<EnemyIntentController>();
                if (intent == null)
                    intent = owner.GetComponentInParent<EnemyIntentController>();
            }
        }

        // 3) 敌人自身 / 子树
        if (intent == null)
        {
            intent = enemy.GetComponent<EnemyIntentController>();
            if (intent == null)
                intent = enemy.GetComponentInParent<EnemyIntentController>();
            if (intent == null)
                intent = enemy.GetComponentInChildren<EnemyIntentController>();
            if (intent == null)
                intent = enemy.gameObject.AddComponent<EnemyIntentController>();
        }

        if (intent != null)
        {
            if (intent.stats == null || intent.stats != enemy)
                intent.stats = enemy;

            // 与 TurnManager 保持一致，避免 HUD / 出牌各用各的实例
            if (TurnManager.Instance != null && TurnManager.Instance.enemyStats == enemy)
                TurnManager.Instance.enemyIntent = intent;
        }

        return intent;
    }

    private void ApplyTargetedEffect(CardDataSO card, CharacterStats enemy, bool qteSuccess, WeaknessType hitWeaknessType)
    {
        if (enemy == null || card == null) return;

        // Play hit animation on the monster
        var animBridge = enemy.GetComponent<MonsterAnimationBridge>();
        if (animBridge == null)
            animBridge = enemy.gameObject.AddComponent<MonsterAnimationBridge>();
        if (animBridge != null)
        {
            animBridge.PlayGetHit();
        }

        int baseDmg = card.effectValue;
        int totalDmg = baseDmg;
        if (qteSuccess)
            totalDmg = baseDmg + Mathf.RoundToInt(baseDmg * qteBonusMultiplier);

        // QTE 成功：跳字同时闪一下弱点色（只闪一次，与主伤害同步）
        if (qteSuccess && hitWeaknessType != WeaknessType.None)
            MonsterHitFlash.Play(enemy, hitWeaknessType);

        switch (card.cardType)
        {
            case CardType.Attack:
            case CardType.Fire:
                ApplyDamageWithPopup(enemy, totalDmg, qteSuccess);
                if (card.specialEffect == CardSpecialEffect.ApplyBurn)
                {
                    int stacks = Mathf.Max(1, card.specialEffectValue);
                    enemy.AddBurn(stacks);
                    Debug.Log($"[CardManager] 【{card.cardName}】附加 {stacks} 层灼烧（当前 {enemy.BurnStacks} 层）。");
                }
                else if (card.specialEffect == CardSpecialEffect.DetonateBurn)
                {
                    int stacks = enemy.ConsumeBurn();
                    int damagePerStack = Mathf.Max(1, card.specialEffectValue);
                    int burstDamage = stacks * damagePerStack;
                    if (burstDamage > 0)
                        ApplyDamageWithPopup(enemy, burstDamage, qteSuccess);
                    Debug.Log($"[CardManager] 【{card.cardName}】引爆 {stacks} 层灼烧，额外造成 {burstDamage} 点伤害。");
                }
                Debug.Log(qteSuccess
                    ? $"[CardManager] QTE 成功！【{card.cardName}】造成 {totalDmg} 点伤害"
                    : $"[CardManager] 【{card.cardName}】造成 {baseDmg} 点伤害");
                break;

            case CardType.ArmorBreak:
                // 黄弱点 QTE 成功：先清空全部护甲，再造成伤害（伤害直接打在血上）
                // 未 QTE：先小额破甲，再正常结算伤害（仍会先吃剩余护甲）
                if (qteSuccess)
                {
                    int before = enemy.CurrentArmor;
                    enemy.ClearArmor();
                    ApplyDamageWithPopup(enemy, totalDmg, true);
                    Debug.Log($"[CardManager] QTE 成功！【{card.cardName}】先清甲 {before}→0，再造成 {totalDmg} 点伤害");
                }
                else
                {
                    int strip = Mathf.Max(2, card.effectValue / 2);
                    int stripped = enemy.StripArmor(strip);
                    ApplyDamageWithPopup(enemy, totalDmg, false);
                    Debug.Log($"[CardManager] 【{card.cardName}】破甲 {stripped}，再造成 {baseDmg} 点伤害");
                }
                if (card.specialEffect == CardSpecialEffect.DetonateBurn)
                {
                    int stacks = enemy.ConsumeBurn();
                    int damagePerStack = Mathf.Max(1, card.specialEffectValue);
                    int burstDamage = stacks * damagePerStack;
                    if (burstDamage > 0)
                        ApplyDamageWithPopup(enemy, burstDamage, qteSuccess);
                    Debug.Log($"[CardManager] 【{card.cardName}】引爆 {stacks} 层灼烧，额外造成 {burstDamage} 点伤害。");
                }
                break;

            case CardType.Seal:
                ApplyDamageWithPopup(enemy, totalDmg, qteSuccess);
                if (qteSuccess)
                {
                    var intent = enemy.GetComponent<EnemyIntentController>();
                    if (intent == null)
                        intent = enemy.GetComponentInParent<EnemyIntentController>();
                    if (intent != null)
                        intent.InterruptCharge();
                    Debug.Log($"[CardManager] QTE 成功！【{card.cardName}】伤害 {totalDmg}，尝试打断蓄力");
                }
                else
                {
                    Debug.Log($"[CardManager] 【{card.cardName}】造成 {baseDmg} 点伤害");
                }
                break;
        }
    }

    private static void ApplyDamageWithPopup(CharacterStats enemy, int damage, bool empowered)
    {
        if (enemy == null || damage <= 0) return;
        enemy.TakeDamage(damage);
        DamagePopup.Show(enemy.transform, damage, empowered);
    }
}
