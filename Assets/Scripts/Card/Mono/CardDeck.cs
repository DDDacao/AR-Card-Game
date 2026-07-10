using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

public class CardDeck : MonoBehaviour
{
    public CardManager cardManager;
    public CardLayoutManager LayoutManager;
    public Vector3 deckPosition;
    public Transform handAnchor; // 手牌在相机底部的挂载点(AR相机子物体)

    private List<CardDataSO> drawDeck = new();  // 抽牌堆
    private List<CardDataSO> discardDeck = new();  // 弃牌堆
    private List<Card> handCardObjectList = new();  // 当前手牌(每回合)

//测试用
    private void Start()
    {
        InitializeDeck();
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

    public void DrawCard(int amount)
{
    for (int i = 0; i < amount; i++)
    {
        if (drawDeck.Count == 0)
        {
            //TODO: 洗牌/更新抽牌堆or弃牌堆的数字
            Debug.LogWarning("抽牌堆已空，无法继续抽牌！");
            return;
        }
        
        CardDataSO currentCardData = drawDeck[0];
        drawDeck.RemoveAt(0);

        var cardObj = cardManager.GetCardObj();
        
        // 递归应用 Card 层，使其归 Overlay 相机渲染
        int cardLayer = LayerMask.NameToLayer("Card");
        if (cardLayer == -1) cardLayer = 6;
        CardCameraManager.SetLayerRecursive(cardObj, cardLayer);

        if (handAnchor != null)
        {
            cardObj.transform.SetParent(handAnchor, false);
        }
        var card = cardObj.GetComponent<Card>();
        // 初始化
        card.Init(currentCardData);
        card.transform.localPosition = deckPosition;

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
            currentCard.transform.DOLocalMove(cardTransform.pos, 0.5f);
        }); // 👈 结尾是 ); 而不是 };

        currentCard.GetComponent<SortingGroup>().sortingOrder = i;
        currentCard.UpdatePosition(cardTransform.pos);
    }
}

    /// <summary>
    /// 弃掉当前所有手牌，将其释放回对象池
    /// </summary>
    public void DiscardHand()
    {
        foreach (var card in handCardObjectList)
        {
            if (card != null)
            {
                cardManager.ReleaseCardObj(card.gameObject);
            }
        }
        handCardObjectList.Clear();
    }

    /// <summary>
    /// 从手牌中移除特定卡牌，并重新排列剩余卡牌
    /// </summary>
    public void RemoveCardFromHand(Card card)
    {
        if (handCardObjectList.Contains(card))
        {
            handCardObjectList.Remove(card);
            // 立即重新排列手牌，延迟设为0
            SetCardLayout(0f);
        }
    }
}