using UnityEngine;
using TMPro; 
using UnityEngine.EventSystems;
using UnityEngine.Rendering; 
using DG.Tweening;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   
    [Header(header:"test")]
    public SpriteRenderer cardSprite;

    public TextMeshPro costText,descriptionText,typeText;

    public CardDataSO cardData;

    [Header(header:"原始数据")]
    public Vector3 originalPosition;

    public int originallayerOrder;

    private Transform visualChild;
    private CardDragHandler dragHandler;

    private void Awake()
    {
        visualChild = transform.Find("Entry");
        dragHandler = GetComponent<CardDragHandler>();
    }

    private void Start() {
        Init(cardData);
    }

    public void Init(CardDataSO data)
    {
        cardData = data;
        if (data == null) return;

        if (cardSprite != null)
            cardSprite.sprite = data.cardImage;
        if (costText != null)
        {
            costText.text = data.cost.ToString();
            TmpChineseFontUtil.Apply(costText, costText.text);
        }
        if (descriptionText != null)
        {
            descriptionText.text = data.description;
            TmpChineseFontUtil.Apply(descriptionText, data.description);
        }
        if (typeText != null)
        {
            string label = GetTypeLabel(data.cardType);
            typeText.text = label;
            TmpChineseFontUtil.Apply(typeText, label);
        }
    }

    private static string GetTypeLabel(CardType type)
    {
        switch (type)
        {
            case CardType.Attack: return "攻击";
            case CardType.Defense: return "防御";
            case CardType.Ability: return "技能";
            case CardType.ArmorBreak: return "破甲";
            case CardType.Seal: return "镇魂";
            case CardType.Fire: return "火符";
            default: return type.ToString();
        }
    }

    public void UpdatePosition(Vector3 position)
    {
        originalPosition = position;
        originallayerOrder = GetComponent<SortingGroup>().sortingOrder;
    }

    public void UpdataPosition(Vector3 position)
    {
        transform.localPosition = position;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dragHandler != null && dragHandler.IsDragging) return;
        if (QTEManager.Instance != null && QTEManager.Instance.IsRunning) return;

        GetComponent<SortingGroup>().sortingOrder = 25;
        if (visualChild != null)
        {
            visualChild.DOKill();
            visualChild.DOLocalMove(new Vector3(0f, 1.2f, 0f), 0.2f).SetEase(Ease.OutCubic);
        }
        else
        {
            transform.DOKill();
            transform.DOLocalMove(originalPosition + Vector3.up, 0.2f).SetEase(Ease.OutCubic);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetCardPosition();
    }

    public void ResetCardPosition()
    {
        GetComponent<SortingGroup>().sortingOrder = originallayerOrder;
        if (visualChild != null)
        {
            visualChild.DOKill();
            visualChild.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
        }
        else
        {
            transform.DOKill();
            transform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutCubic);
        }
        transform.localPosition = originalPosition;
    }
}

