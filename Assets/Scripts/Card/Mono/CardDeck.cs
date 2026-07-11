using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

/// <summary>
/// 符匣：固定顺序发牌，用后本场消耗，不洗牌、无弃牌堆回灌。
/// </summary>
public class CardDeck : MonoBehaviour
{
    public const int DefaultHandLimit = 6;

    public CardManager cardManager;
    public CardLayoutManager LayoutManager;
    public Vector3 deckPosition;
    public Transform handAnchor;

    [Header("符匣固定顺序（优先）")]
    [Tooltip("配置后按列表顺序抽牌；为空则回退到 CardLibrary 数量展开")]
    public FuXiaOrderSO fuXiaOrder;

    [Tooltip("为 true 且配置了 FuXiaOrder 时使用固定顺序")]
    public bool useFixedOrder = true;

    [Tooltip("运行时插入符匣头部的牌（奖励符等，不写入 SO）")]
    public List<CardDataSO> runtimePrefixCards = new List<CardDataSO>();

    [Header("手牌上限")]
    public int handLimit = DefaultHandLimit;

    [Header("手牌显示缩放（横屏）")]
    [Range(0.4f, 1.2f)]
    public float handCardScale = 0.45f;

    private List<CardDataSO> drawDeck = new();   // 符匣剩余
    private List<Card> handCardObjectList = new();
    private List<CardDataSO> consumedThisBattle = new(); // 本场已使用（仅记录）

    public int HandCount => handCardObjectList.Count;
    public int DrawDeckCount => drawDeck.Count;
    public int HandLimit => handLimit;
    public int ConsumedCount => consumedThisBattle.Count;
    public FuXiaOrderSO ActiveFuXia => fuXiaOrder;

    public void InitializeDeck()
    {
        drawDeck.Clear();
        handCardObjectList.Clear();
        consumedThisBattle.Clear();

        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();

        if (useFixedOrder && fuXiaOrder != null && fuXiaOrder.TotalCount > 0)
        {
            drawDeck = fuXiaOrder.CreateRuntimeQueue();
            // 奖励符插入头部（本场可抽到）
            if (runtimePrefixCards != null && runtimePrefixCards.Count > 0)
            {
                for (int i = runtimePrefixCards.Count - 1; i >= 0; i--)
                {
                    if (runtimePrefixCards[i] != null)
                        drawDeck.Insert(0, runtimePrefixCards[i]);
                }
            }
            Debug.Log($"[CardDeck] 符匣固定顺序加载：{fuXiaOrder.displayName}，共 {drawDeck.Count} 张（含奖励前缀 {runtimePrefixCards?.Count ?? 0}）");
            return;
        }

        // 回退：从 CardLibrary 按 amount 展开（顺序即列表顺序，仍不洗牌）
        if (cardManager == null || cardManager.currentLibrary == null)
        {
            Debug.LogError("[CardDeck] 未配置 FuXiaOrder，且 CardManager/currentLibrary 无效。");
            return;
        }

        foreach (var entry in cardManager.currentLibrary.cardLibraryList)
        {
            if (entry.cardData == null) continue;
            for (int i = 0; i < entry.amount; i++)
                drawDeck.Add(entry.cardData);
        }
        Debug.Log($"[CardDeck] 使用牌库展开（无洗牌），共 {drawDeck.Count} 张");
    }

    /// <summary>
    /// 开局手牌：固定顺序时取 SO 配置的 initialHandSize，否则用传入 amount。
    /// </summary>
    public int DrawInitialHand(int fallbackAmount = 4)
    {
        int amount = fallbackAmount;
        if (useFixedOrder && fuXiaOrder != null && fuXiaOrder.initialHandSize > 0)
            amount = fuXiaOrder.initialHandSize;
        return DrawCard(amount);
    }

    [ContextMenu("TestDrawCard")]
    public void TestDrawCard()
    {
        DrawCard(1);
    }

    public int DrawCard(int amount)
    {
        int drawn = 0;
        for (int i = 0; i < amount; i++)
        {
            if (handCardObjectList.Count >= handLimit)
            {
                Debug.Log("[CardDeck] 手牌已达上限，停止抽牌。");
                break;
            }

            if (drawDeck.Count == 0)
            {
                Debug.LogWarning("[CardDeck] 符匣已空，无法继续补牌！");
                break;
            }

            CardDataSO currentCardData = drawDeck[0];
            drawDeck.RemoveAt(0);

            var cardObj = cardManager.GetCardObj();

            int cardLayer = LayerMask.NameToLayer("Card");
            if (cardLayer == -1) cardLayer = 6;
            CardCameraManager.SetLayerRecursive(cardObj, cardLayer);

            if (handAnchor != null)
                cardObj.transform.SetParent(handAnchor, false);

            var card = cardObj.GetComponent<Card>();
            card.Init(currentCardData);
            card.transform.localPosition = deckPosition;
            card.transform.localScale = Vector3.zero;

            handCardObjectList.Add(card);
            var delay = drawn * 0.15f;
            SetCardLayout(delay);
            drawn++;
        }

        return drawn;
    }

    public int DrawUpTo(int targetHandCount)
    {
        targetHandCount = Mathf.Min(targetHandCount, handLimit);
        int need = targetHandCount - handCardObjectList.Count;
        if (need <= 0) return 0;
        return DrawCard(need);
    }

    public int DrawRespectingHandLimit(int amount)
    {
        int room = handLimit - handCardObjectList.Count;
        if (room <= 0) return 0;
        return DrawCard(Mathf.Min(amount, room));
    }

    private void SetCardLayout(float delay)
    {
        for (int i = 0; i < handCardObjectList.Count; i++)
        {
            Card currentCard = handCardObjectList[i];
            if (currentCard == null) continue;

            CardTransform cardTransform = LayoutManager.GetCardTransform(i, handCardObjectList.Count);

            currentCard.transform.DOKill();
            Vector3 targetScale = Vector3.one * handCardScale;
            currentCard.transform.DOScale(targetScale, 0.35f).SetDelay(delay).OnComplete(() =>
            {
                if (currentCard != null)
                    currentCard.transform.DOLocalMove(cardTransform.pos, 0.35f);
            });

            var sorting = currentCard.GetComponent<SortingGroup>();
            if (sorting != null)
                sorting.sortingOrder = i;
            currentCard.UpdatePosition(cardTransform.pos);
        }
    }

    public void DiscardHand()
    {
        foreach (var card in handCardObjectList)
        {
            if (card != null)
                cardManager.ReleaseCardObj(card.gameObject);
        }
        handCardObjectList.Clear();
    }

    public void ClearAllForBattleReset()
    {
        DiscardHand();
        drawDeck.Clear();
        consumedThisBattle.Clear();
    }

    /// <summary>
    /// 打出卡牌：从手牌移除（本场消耗，不回符匣）
    /// </summary>
    public void RemoveCardFromHand(Card card)
    {
        if (card != null && card.cardData != null)
            consumedThisBattle.Add(card.cardData);

        if (handCardObjectList.Contains(card))
        {
            handCardObjectList.Remove(card);
            SetCardLayout(0f);
        }
    }
}
