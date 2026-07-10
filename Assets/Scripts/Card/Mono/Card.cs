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
        cardSprite.sprite = data.cardImage;
        costText.text = data.cost.ToString();
        descriptionText.text = data.description;
        typeText.text = data.cardType.ToString();
    }

    public void UpdatePosition(Vector3 position)
    {
        originalPosition = position;
        originallayerOrder = GetComponent<SortingGroup>().sortingOrder;
    }

    public void UpdataPosition(Vector3 position)
    {
        transform.position = position;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.position = originalPosition+Vector3.up;
        GetComponent<SortingGroup>().sortingOrder = 25;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetCardPosition();
    }

    public void ResetCardPosition()
    {
        transform.position = originalPosition;
        GetComponent<SortingGroup>().sortingOrder = originallayerOrder;
    }
}
