using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

public class CardDeck : MonoBehaviour
{
    public CardManager cardManager;
    public CardLayoutManager LayoutManager;
    public Vector3 deckPosition;

    private List<CardDataSO> drawDeck = new();  // 抽牌堆
    private List<CardDataSO> discardDeck = new();  // 弃牌堆
    private List<Card> handCardObjectList = new();  // 当前手牌(每回合)

//测试用
    private void Start()
    {
        InitializeDeck();

        DrawCard(3);
    }

    public void InitializeDeck()
    {
        drawDeck.Clear();
        foreach (var entry in cardManager.currentLibrary.cardLibraryList)
        {
            for (int i = 0; i < entry.amount; i++)
            {
                drawDeck.Add(entry.cardData);
            }
        }

        //TODO: 洗牌/更新抽牌堆or弃牌堆的数字
    }
[ContextMenu("TestDrawCard")]
public void TestDrawCard(){
    DrawCard(1);
}

    private void DrawCard(int amount)
{
    for (int i = 0; i < amount; i++)
    {
        if (drawDeck.Count == 0)
        {
            //TODO: 洗牌/更新抽牌堆or弃牌堆的数字
        }
        
        CardDataSO currentCardData = drawDeck[0];
        drawDeck.RemoveAt(0);

        var card = cardManager.GetCardObj().GetComponent<Card>();
        // 初始化
        card.Init(currentCardData);
        card.transform.position = deckPosition;

        handCardObjectList.Add(card);
        var delay = i * 0.5f;
         SetCardLayout(delay);
    }
   
}

private void SetCardLayout(float delay)
{
    for (int i = 0; i < handCardObjectList.Count; i++)
    {
        Card currentCard = handCardObjectList[i];

        CardTransform cardTransform = LayoutManager.GetCardTransform(i, handCardObjectList.Count);

        // currentCard.transform.SetPositionAndRotation(cardTransform.pos, cardTransform.rotation);
        
       currentCard.transform.DOScale(Vector3.one, 0.5f).SetDelay(delay).OnComplete(() =>
        {
        currentCard.transform.DOMove(cardTransform.pos, 0.5f);
        }); // 👈 结尾是 ); 而不是 };

        currentCard.GetComponent<SortingGroup>().sortingOrder = i;
        currentCard.UpdatePosition(cardTransform.pos);
    }
}

}