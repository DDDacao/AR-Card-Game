using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour,IBeginDragHandler,IEndDragHandler,IDragHandler
{
    public GameObject arrowPrefab;
    private GameObject currentArrow;


    private Card currentCard;

    private bool canMove;

    private bool canExecute;

    private void Awake()
    {
        currentCard = GetComponent<Card>();
    }

       public void OnBeginDrag(PointerEventData eventData)
    {
        switch (currentCard.cardData.cardType)
        {
            case CardType.Attack:
            currentArrow = Instantiate(arrowPrefab,transform.position,Quaternion.identity);
                break;
               
            case CardType.Defense:
            case CardType.Ability:
                canMove = true;
                break;
               
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(canMove)
        {
            Vector3 screenPos = new(Input.mousePosition.x,Input.mousePosition.y,10);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            currentCard.transform.position = worldPos;
            canExecute = worldPos.y>0.5f;
            
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(currentArrow!=null)
        {
            Destroy(currentArrow);
        }
        if(canExecute)
        {
            //todo
        }else
        {
            currentCard.ResetCardPosition();
        }
        
    }
    
}
