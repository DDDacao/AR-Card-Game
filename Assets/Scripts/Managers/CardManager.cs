using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    public PoolTool poolTool;
    public List<CardDataSO> cardDataList;  // 游戏中所有可能出现的卡牌

    public CardLibrarySO newGameCardLibrary; // 新游戏时使用的卡牌库

    public CardLibrarySO currentLibrary;     // 当前使用的卡牌库

    private void Awake()
    {
        Instance = this;

        InitializeCardDataList();

        foreach(var item in newGameCardLibrary.cardLibraryList){
            currentLibrary.cardLibraryList.Add(item); 
        } 
    }

    private void OnDisable() {
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

public GameObject GetCardObj(){

    var cardObj = poolTool.GetObj();
    cardObj.transform.localScale = Vector3.zero;
    
    return cardObj;
}
public void ReleaseCardObj(GameObject obj){
    poolTool.ReleaseObj(obj);
}

/// <summary>
/// 执行卡牌效果的主入口 (集中判定模式)
/// </summary>
public void ExecuteCard(CardDataSO card, CharacterStats player, CharacterStats enemy)
{
    if (card == null) return;

    switch (card.cardType)
    {
        case CardType.Attack:
            if (enemy != null)
            {
                enemy.TakeDamage(card.effectValue);
                Debug.Log($"[CardManager] 使用攻击牌【{card.cardName}】，对 {enemy.gameObject.name} 造成了 {card.effectValue} 点伤害！");
            }
            else
            {
                Debug.LogWarning("[CardManager] 攻击失败，当前没有目标敌人！");
            }
            break;

        case CardType.Defense:
            if (player != null)
            {
                player.AddArmor(card.effectValue);
                Debug.Log($"[CardManager] 使用防御牌【{card.cardName}】，主角获得 {card.effectValue} 点护甲！");
            }
            break;

        case CardType.Ability:
            // 比如聚气诀
            if (card.cardName == "聚气诀" && player != null)
            {
                player.AddEnergy(card.effectValue);
                Debug.Log($"[CardManager] 使用技能牌【{card.cardName}】，主角回复了 {card.effectValue} 点灵气！");
            }
            break;
    }
}
}