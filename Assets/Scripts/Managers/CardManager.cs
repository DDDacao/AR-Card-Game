using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CardManager : MonoBehaviour
{
    public PoolTool poolTool;
    public List<CardDataSO> cardDataList;  // 游戏中所有可能出现的卡牌

    public CardLibrarySO newGameCardLibrary; // 新游戏时使用的卡牌库

    public CardLibrarySO currentLibrary;     // 当前使用的卡牌库



    private void Awake()
    {
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
}