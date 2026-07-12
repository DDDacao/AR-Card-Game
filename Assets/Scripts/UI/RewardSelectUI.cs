using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 奖励三选一：实例化与手牌相同的 Card 预制体。
/// 展示时把 Canvas 临时改为 Screen Space - Camera，
/// 使 CardCamera（Overlay）画在遮罩之上，卡牌不会被压黑。
/// </summary>
public class RewardSelectUI : MonoBehaviour, IPointerClickHandler
{
    public GameObject root;
    public TextMeshProUGUI titleText;
    public Transform buttonContainer;

    [Header("手牌预制体（空则从 CardManager.poolTool.objPrefab 取）")]
    public GameObject cardPrefab;

    [Header("展示")]
    public float displayScale = 0.55f;
    public float worldSpacing = 2.4f;
    public Vector3 viewLocalCenter = new Vector3(0f, 0.15f, 7.5f);

    [Header("遮罩")]
    [Tooltip("奖励展示时全屏压暗透明度（卡在遮罩前，可略深）")]
    [Range(0.2f, 0.85f)]
    public float dimAlpha = 0.55f;

    // 兼容旧序列化字段
    public Sprite cardBaseSprite;
    public Sprite circleFrameSprite;
    public Sprite typeBannerSprite;
    public Sprite costBadgeSprite;
    public List<Button> choiceButtons = new List<Button>();
    public List<TextMeshProUGUI> choiceLabels = new List<TextMeshProUGUI>();
    public List<Image> choiceCardImages = new List<Image>();
    public List<TextMeshProUGUI> choiceNameLabels = new List<TextMeshProUGUI>();
    public Vector2 cardSize = new Vector2(280f, 400f);
    public float cardSpacing = 36f;

    private Action<CardDataSO> onPicked;
    private List<CardDataSO> options;
    private readonly List<GameObject> spawnedCards = new List<GameObject>();
    private readonly List<bool> handWasActive = new List<bool>();
    private readonly List<GameObject> hiddenHandCards = new List<GameObject>();

    private Image dimImage;
    private bool dimRaycastWas;
    private Color dimColorWas;
    private Color panelColorWas;
    private bool panelRaycastWas;
    private Image panelImage;

    // 临时改 Canvas，让卡牌相机画在 UI 上面
    private Canvas canvas;
    private RenderMode canvasModeWas;
    private Camera canvasCamWas;
    private float canvasPlaneWas;
    private bool canvasOverridden;

    private void Awake()
    {
        if (root == null) root = gameObject;
        dimImage = GetComponent<Image>();
        if (dimImage == null && root != null)
            dimImage = root.GetComponent<Image>();

        var panelTf = root != null ? root.transform.Find("Panel") : null;
        if (panelTf != null)
            panelImage = panelTf.GetComponent<Image>();

        Hide();
    }

    public void Show(List<CardDataSO> rewards, Action<CardDataSO> onPick)
    {
        options = rewards;
        onPicked = onPick;

        gameObject.SetActive(true);
        if (root == null) root = gameObject;
        root.SetActive(true);
        transform.SetAsLastSibling();
        if (root.transform != transform)
            root.transform.SetAsLastSibling();

        // 关键：Screen Space Overlay 永远盖在所有相机之上，世界/卡牌相机里的卡会变黑。
        // 展示奖励时改为 Screen Space - Camera，CardCamera 作为 Overlay 画在 UI 之后。
        BeginCanvasInFrontOfCards();

        if (dimImage != null)
        {
            dimRaycastWas = dimImage.raycastTarget;
            dimColorWas = dimImage.color;
            dimImage.raycastTarget = true;
            var c = dimImage.color;
            c.a = dimAlpha;
            dimImage.color = c;
        }

        // 中间 Panel 几乎不透明会整块挡住卡；展示时关掉它的底色
        if (panelImage != null)
        {
            panelColorWas = panelImage.color;
            panelRaycastWas = panelImage.raycastTarget;
            panelImage.color = new Color(0f, 0f, 0f, 0f);
            panelImage.raycastTarget = false;
        }

        SetChildrenRaycastExceptRootDim(false);

        if (titleText != null)
        {
            titleText.text = "选择一张奖励符咒";
            TmpChineseFontUtil.Apply(titleText, titleText.text);
            titleText.raycastTarget = false;
        }

        ClearLegacyUiChoices();
        ClearSpawnedCards();
        HideHandCards();

        int count = rewards != null ? Mathf.Min(3, rewards.Count) : 0;
        GameObject prefab = ResolveCardPrefab();
        if (prefab == null)
        {
            Debug.LogError("[RewardSelectUI] 找不到 Card 预制体，无法展示奖励。");
            return;
        }

        Camera cardCam = GetCardCamera();
        for (int i = 0; i < count; i++)
        {
            if (rewards[i] == null) continue;
            SpawnRewardCard(prefab, rewards[i], i, count, cardCam);
        }
    }

    public void Hide()
    {
        ClearSpawnedCards();
        RestoreHandCards();
        EndCanvasInFrontOfCards();

        if (dimImage != null)
        {
            dimImage.raycastTarget = dimRaycastWas;
            dimImage.color = dimColorWas;
        }

        if (panelImage != null)
        {
            panelImage.color = panelColorWas;
            panelImage.raycastTarget = panelRaycastWas;
        }

        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void BeginCanvasInFrontOfCards()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 根 Canvas
        while (canvas.rootCanvas != null && canvas.rootCanvas != canvas)
            canvas = canvas.rootCanvas;

        canvasModeWas = canvas.renderMode;
        canvasCamWas = canvas.worldCamera;
        canvasPlaneWas = canvas.planeDistance;
        canvasOverridden = true;

        Camera main = Camera.main;
        if (main == null && CardCameraManager.Instance != null)
            main = CardCameraManager.Instance.mainCamera;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = main;
        // 稍远一点，避免近裁切；卡牌相机 Overlay 仍会画在整帧之后
        canvas.planeDistance = Mathf.Clamp(main != null ? main.nearClipPlane + 2f : 2f, 1f, 50f);

        // 确保 CardCamera 在 Stack 最末，画在 UI 上面
        EnsureCardCameraOnTopOfStack(main);
    }

    private void EndCanvasInFrontOfCards()
    {
        if (!canvasOverridden || canvas == null) return;
        canvas.renderMode = canvasModeWas;
        canvas.worldCamera = canvasCamWas;
        canvas.planeDistance = canvasPlaneWas;
        canvasOverridden = false;
        canvas = null;
    }

    private static void EnsureCardCameraOnTopOfStack(Camera main)
    {
        if (main == null) return;
        var cardCam = GetCardCamera();
        if (cardCam == null) return;

        var mainData = main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (mainData == null) return;

        var stack = mainData.cameraStack;
        if (stack == null) return;

        // 挪到最后
        if (stack.Contains(cardCam))
            stack.Remove(cardCam);
        stack.Add(cardCam);
    }

    /// <summary>点遮罩空白处时，射线选卡（备用）。</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        TryPickFromScreen(eventData.position);
    }

    public void TryPickFromScreen(Vector2 screenPos)
    {
        if (spawnedCards.Count == 0) return;
        Camera cardCam = GetCardCamera();
        if (cardCam == null) return;

        Ray ray = cardCam.ScreenPointToRay(screenPos);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 200f);
        if (hit.collider == null) return;

        var proxy = hit.collider.GetComponent<RewardCardPickProxy>();
        if (proxy == null)
            proxy = hit.collider.GetComponentInParent<RewardCardPickProxy>();
        if (proxy != null)
            Pick(proxy.optionIndex);
    }

    private void SpawnRewardCard(GameObject prefab, CardDataSO data, int index, int total, Camera cardCam)
    {
        GameObject cardObj = Instantiate(prefab);
        cardObj.name = "RewardCard_" + (data != null ? data.cardName : index.ToString());
        cardObj.SetActive(true);

        int cardLayer = LayerMask.NameToLayer("Card");
        if (cardLayer < 0) cardLayer = 6;
        CardCameraManager.SetLayerRecursive(cardObj, cardLayer);

        var drag = cardObj.GetComponent<CardDragHandler>();
        if (drag != null) drag.enabled = false;

        var card = cardObj.GetComponent<Card>();
        if (card != null)
            card.Init(data);

        var proxy = cardObj.GetComponent<RewardCardPickProxy>();
        if (proxy == null) proxy = cardObj.AddComponent<RewardCardPickProxy>();
        proxy.optionIndex = index;
        proxy.owner = this;

        var col = cardObj.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = cardObj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 4f);
            col = box;
        }
        col.enabled = true;

        float offset = (index - (total - 1) * 0.5f) * worldSpacing;
        Vector3 local = viewLocalCenter + new Vector3(offset, 0f, 0f);
        if (cardCam != null)
        {
            cardObj.transform.SetParent(null, true);
            cardObj.transform.position = cardCam.transform.TransformPoint(local);
            cardObj.transform.rotation = cardCam.transform.rotation;
        }
        else
        {
            cardObj.transform.position = new Vector3(offset, 0.5f, 0f);
        }

        cardObj.transform.localScale = Vector3.one * displayScale;

        var sg = cardObj.GetComponent<SortingGroup>();
        if (sg != null) sg.sortingOrder = 50 + index;

        if (card != null)
            card.UpdatePosition(cardObj.transform.position);

        spawnedCards.Add(cardObj);
    }

    private GameObject ResolveCardPrefab()
    {
        if (cardPrefab != null) return cardPrefab;

        var cm = CardManager.Instance != null
            ? CardManager.Instance
            : FindAnyObjectByType<CardManager>();
        if (cm != null && cm.poolTool != null && cm.poolTool.objPrefab != null)
            return cm.poolTool.objPrefab;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Card/Card.prefab");
#else
        return null;
#endif
    }

    private static Camera GetCardCamera()
    {
        if (CardCameraManager.Instance != null && CardCameraManager.Instance.cardCamera != null)
            return CardCameraManager.Instance.cardCamera;
        return Camera.main;
    }

    private void HideHandCards()
    {
        hiddenHandCards.Clear();
        handWasActive.Clear();

        var allCards = FindObjectsByType<Card>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < allCards.Length; i++)
        {
            var c = allCards[i];
            if (c == null) continue;
            if (c.GetComponent<RewardCardPickProxy>() != null) continue;
            hiddenHandCards.Add(c.gameObject);
            handWasActive.Add(c.gameObject.activeSelf);
            c.gameObject.SetActive(false);
        }
    }

    private void RestoreHandCards()
    {
        for (int i = 0; i < hiddenHandCards.Count; i++)
        {
            if (hiddenHandCards[i] == null) continue;
            bool was = i < handWasActive.Count && handWasActive[i];
            hiddenHandCards[i].SetActive(was);
        }
        hiddenHandCards.Clear();
        handWasActive.Clear();
    }

    private void ClearSpawnedCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i]);
        }
        spawnedCards.Clear();
    }

    private void ClearLegacyUiChoices()
    {
        choiceButtons.Clear();
        choiceLabels.Clear();
        choiceCardImages.Clear();
        choiceNameLabels.Clear();

        if (buttonContainer == null) return;
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
        {
            var child = buttonContainer.GetChild(i);
            if (child != null && child.name.StartsWith("RewardChoice"))
                Destroy(child.gameObject);
        }
    }

    private void SetChildrenRaycastExceptRootDim(bool enable)
    {
        if (root == null) return;
        var images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var img = images[i];
            if (img == null || img == dimImage) continue;
            img.raycastTarget = enable;
        }
        if (dimImage != null) dimImage.raycastTarget = true;
    }

    public void Pick(int index)
    {
        CardDataSO card = options != null && index >= 0 && index < options.Count ? options[index] : null;
        Hide();
        onPicked?.Invoke(card);
        onPicked = null;
    }
}

/// <summary>挂在奖励展示用的 Card 实例上；支持直接点卡。</summary>
public class RewardCardPickProxy : MonoBehaviour, IPointerClickHandler
{
    public int optionIndex;
    public RewardSelectUI owner;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.Pick(optionIndex);
    }
}
