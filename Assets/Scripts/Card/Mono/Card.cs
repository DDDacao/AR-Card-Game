using UnityEngine;
using TMPro; 
using UnityEngine.EventSystems;
using UnityEngine.Rendering; 

public class Card : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler
{
   
    [Header(header:"test")]
    public SpriteRenderer cardSprite;

    public TextMeshPro costText,descriptionText,typeText;

    public CardDataSO cardData;

    [Header(header:"原始数据")]
    public Vector3 originalPosition;

    public int originallayerOrder;

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
            costText.text = data.cost.ToString();
        if (descriptionText != null)
            descriptionText.text = data.description;
        if (typeText != null)
            typeText.text = GetTypeLabel(data.cardType);
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
        transform.localPosition = originalPosition + Vector3.up;
        GetComponent<SortingGroup>().sortingOrder = 25;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetCardPosition();
    }

    public void ResetCardPosition()
    {
        transform.localPosition = originalPosition;
        GetComponent<SortingGroup>().sortingOrder = originallayerOrder;
    }
}
