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

        bool canQte = hitWeakness
                      && weakness != null
                      && card.ResolveWeaknessTag() != WeaknessType.None
                      && weakness.weaknessType == card.ResolveWeaknessTag();

        if (canQte && QTEManager.Instance != null)
        {
            var weaknessType = weakness.weaknessType;
            Debug.Log($"[CardManager] 命中弱点【{weaknessType}】，触发 QTE！牌：{card.cardName}");
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
            var tag = hitWeakness && weakness != null ? weakness.weaknessType : WeaknessType.None;
            ApplyTargetedEffect(card, enemy, false, tag);
            if (TurnManager.Instance != null)
                TurnManager.Instance.CheckBattleEnd();
        }
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
                // 破甲：先结算伤害；QTE 成功再额外破甲（effectValue2，默认等于 effectValue）
                ApplyDamageWithPopup(enemy, totalDmg, qteSuccess);
                if (card.specialEffect == CardSpecialEffect.DetonateBurn)
                {
                    int stacks = enemy.ConsumeBurn();
                    int damagePerStack = Mathf.Max(1, card.specialEffectValue);
                    int burstDamage = stacks * damagePerStack;
                    if (burstDamage > 0)
                        ApplyDamageWithPopup(enemy, burstDamage, qteSuccess);
                    Debug.Log($"[CardManager] 【{card.cardName}】引爆 {stacks} 层灼烧，额外造成 {burstDamage} 点伤害。");
                }
                if (qteSuccess)
                {
                    int strip = card.effectValue2 > 0 ? card.effectValue2 : card.effectValue;
                    int stripped = enemy.StripArmor(strip);
                    Debug.Log($"[CardManager] QTE 成功！【{card.cardName}】伤害 {totalDmg}，破甲 {stripped}");
                }
                else
                {
                    // 未 QTE 时也对护甲有小额破甲（策划：对护甲额外有效）
                    int strip = Mathf.Max(2, card.effectValue / 2);
                    int stripped = enemy.StripArmor(strip);
                    Debug.Log($"[CardManager] 【{card.cardName}】伤害 {baseDmg}，破甲 {stripped}");
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
