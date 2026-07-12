using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

/// <summary>
/// 符匣：固定顺序发牌；打出后本场从手牌移除。
/// 牌序列抽完后可按同一设计顺序循环补牌（避免后期无牌可抽）。
/// 怪物意图侧由 EnemyIntentController 对 intentLoop 取模循环。
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

    [Tooltip("旧版兼容字段。奖励卡请改用 runtimeRewardInsertions 指定固定插入时机。")]
    public List<CardDataSO> runtimePrefixCards = new List<CardDataSO>();

    [Tooltip("运行时已获得的奖励卡；由战役流程提供，不写入 SO。")]
    public List<CardDataSO> runtimeEarnedRewards = new List<CardDataSO>();

    [Tooltip("奖励卡插入基础符匣的固定时机；由关卡 SO 提供，不写入 SO。")]
    public List<BattleStageSO.RewardCardInsertion> runtimeRewardInsertions = new List<BattleStageSO.RewardCardInsertion>();

    [Header("循环补牌（走完设计序列后）")]
    [Tooltip("牌堆抽空后，按本场「基础固定顺序」再循环一轮，保证长战斗仍有牌可发")]
    public bool loopDeckWhenEmpty = true;

    [Tooltip("循环时是否把本场已获得的奖励卡也追加进序列（玩家构筑可持续用）")]
    public bool includeEarnedRewardsInLoop = true;

    [Header("手牌上限")]
    public int handLimit = DefaultHandLimit;

    [Header("手牌显示缩放（横屏）")]
    [Range(0.4f, 1.2f)]
    public float handCardScale = 0.45f;

    private List<CardDataSO> drawDeck = new();   // 符匣剩余
    private List<CardDataSO> loopSourceSequence = new(); // 循环用的基础序列快照
    private List<Card> handCardObjectList = new();
    private List<CardDataSO> consumedThisBattle = new(); // 本场已使用（仅记录）
    private readonly HashSet<int> usedRewardInsertionIndexes = new();
    private int baseCardsDrawn;
    private int deckLoopCount;

    public int HandCount => handCardObjectList.Count;
    public int DrawDeckCount => drawDeck.Count;
    public int HandLimit => handLimit;
    public int ConsumedCount => consumedThisBattle.Count;
    public int DeckLoopCount => deckLoopCount;
    public FuXiaOrderSO ActiveFuXia => fuXiaOrder;

    public void InitializeDeck()
    {
        drawDeck.Clear();
        loopSourceSequence.Clear();
        handCardObjectList.Clear();
        consumedThisBattle.Clear();
        usedRewardInsertionIndexes.Clear();
        baseCardsDrawn = 0;
        deckLoopCount = 0;

        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();

        if (useFixedOrder && fuXiaOrder != null && fuXiaOrder.TotalCount > 0)
        {
            drawDeck = fuXiaOrder.CreateRuntimeQueue();
            // 旧版前缀保留兼容，正式战役使用固定插入时机。
            if (runtimePrefixCards != null && runtimePrefixCards.Count > 0)
            {
                for (int i = runtimePrefixCards.Count - 1; i >= 0; i--)
                {
                    if (runtimePrefixCards[i] != null)
                        drawDeck.Insert(0, runtimePrefixCards[i]);
                }
            }
            // 快照基础序列，供抽空后循环（不含「一次性」奖励插入，奖励由 includeEarnedRewardsInLoop 处理）
            loopSourceSequence = new List<CardDataSO>(drawDeck);
            Debug.Log($"[CardDeck] 符匣固定顺序加载：{fuXiaOrder.displayName}，基础 {drawDeck.Count} 张，循环={(loopDeckWhenEmpty ? "开" : "关")}，奖励插入点 {runtimeRewardInsertions?.Count ?? 0} 个。");
            return;
        }

        // 回退：从 CardLibrary 按 amount 展开（顺序即列表顺序）
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
        loopSourceSequence = new List<CardDataSO>(drawDeck);
        Debug.Log($"[CardDeck] 使用牌库展开，共 {drawDeck.Count} 张，循环={(loopDeckWhenEmpty ? "开" : "关")}");
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

            if (!TryTakeNextCard(out CardDataSO currentCardData))
            {
                Debug.LogWarning("[CardDeck] 符匣已空，无法继续补牌！");
                break;
            }

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

    private bool TryTakeNextCard(out CardDataSO card)
    {
        card = null;

        // 一次性奖励插入（按关卡计划，仅第一轮序列生效）
        if (runtimeRewardInsertions != null)
        {
            for (int i = 0; i < runtimeRewardInsertions.Count; i++)
            {
                if (usedRewardInsertionIndexes.Contains(i)) continue;
                var insertion = runtimeRewardInsertions[i];
                if (insertion == null || insertion.afterBaseCardsDrawn != baseCardsDrawn) continue;

                usedRewardInsertionIndexes.Add(i);
                if (runtimeEarnedRewards != null
                    && insertion.earnedRewardIndex >= 0
                    && insertion.earnedRewardIndex < runtimeEarnedRewards.Count
                    && runtimeEarnedRewards[insertion.earnedRewardIndex] != null)
                {
                    card = runtimeEarnedRewards[insertion.earnedRewardIndex];
                    Debug.Log($"[CardDeck] 按关卡计划插入奖励符【{card.cardName}】（基础已抽 {baseCardsDrawn} 张）。");
                    return true;
                }

                Debug.LogWarning($"[CardDeck] 奖励插入点缺少第 {insertion.earnedRewardIndex + 1} 张奖励，跳过。");
            }
        }

        if (drawDeck.Count == 0)
            TryRefillDeckLoop();

        if (drawDeck.Count == 0) return false;
        card = drawDeck[0];
        drawDeck.RemoveAt(0);
        baseCardsDrawn++;
        return card != null;
    }

    /// <summary>
    /// 设计序列抽空后：按同一固定顺序再装一轮（可选附带已获奖励卡）。
    /// </summary>
    private void TryRefillDeckLoop()
    {
        if (!loopDeckWhenEmpty) return;

        if (loopSourceSequence == null || loopSourceSequence.Count == 0)
        {
            if (useFixedOrder && fuXiaOrder != null && fuXiaOrder.TotalCount > 0)
                loopSourceSequence = fuXiaOrder.CreateRuntimeQueue();
        }

        if (loopSourceSequence == null || loopSourceSequence.Count == 0)
        {
            Debug.LogWarning("[CardDeck] 无法循环补牌：循环源序列为空。");
            return;
        }

        for (int i = 0; i < loopSourceSequence.Count; i++)
        {
            if (loopSourceSequence[i] != null)
                drawDeck.Add(loopSourceSequence[i]);
        }

        if (includeEarnedRewardsInLoop && runtimeEarnedRewards != null)
        {
            for (int i = 0; i < runtimeEarnedRewards.Count; i++)
            {
                if (runtimeEarnedRewards[i] != null)
                    drawDeck.Add(runtimeEarnedRewards[i]);
            }
        }

        deckLoopCount++;
        Debug.Log($"[CardDeck] 符匣循环第 {deckLoopCount} 轮，补入 {drawDeck.Count} 张（基础序列 {loopSourceSequence.Count} + 奖励循环）。");
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

    private void Update()
    {
        if (handAnchor != null)
        {
            Camera mainCam = (CardCameraManager.Instance != null && CardCameraManager.Instance.mainCamera != null)
                ? CardCameraManager.Instance.mainCamera
                : Camera.main;

            if (mainCam != null)
            {
                // 1. 根据屏幕固定百分比（屏幕底部向上 17.5% 处）和深度 Z=4，计算卡牌期望停留的绝对世界位置
                float targetScreenYPercentage = 0.175f;
                Vector3 cardTargetScreenPos = new Vector3(Screen.width * 0.5f, Screen.height * targetScreenYPercentage, 4f);
                Vector3 cardTargetWorldPos = mainCam.ScreenToWorldPoint(cardTargetScreenPos);

                // 2. 获取 CardLayoutManager 配置的局部 Y 轴偏移量
                float localOffsetY = (LayoutManager != null) ? LayoutManager.centerPoint.y : -2.8f;

                // 3. 结合 HandAnchor 的局部缩放，对 HandAnchor 的位置进行反向补偿
                float scaleY = handAnchor.localScale.y;
                float worldOffset = localOffsetY * scaleY;

                handAnchor.position = cardTargetWorldPos - mainCam.transform.up * worldOffset;
            }
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
        loopSourceSequence.Clear();
        consumedThisBattle.Clear();
        usedRewardInsertionIndexes.Clear();
        baseCardsDrawn = 0;
        deckLoopCount = 0;
    }

    /// <summary>
    /// 打出卡牌：从手牌移除。本场打出的牌记入消耗；牌堆侧靠「循环设计序列」持续补牌。
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
