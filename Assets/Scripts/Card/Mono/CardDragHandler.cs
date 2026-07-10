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
        // 确保当前是玩家回合，否则不允许拖拽出牌
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
        {
            Debug.LogWarning("[CardDragHandler] 现在是怪物回合，不能出牌！");
            return;
        }

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

        bool isAttack = currentCard.cardData.cardType == CardType.Attack;
        CharacterStats targetEnemy = null;

        if (isAttack)
        {
            // 攻击卡：从鼠标松开的屏幕位置发射射线，检测是否指向了敌人
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // 优先进行 3D 射线检测
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                CharacterStats hitStats = hit.collider.GetComponentInParent<CharacterStats>();
                if (hitStats != null && hitStats.gameObject.name != "Player")
                {
                    targetEnemy = hitStats;
                    canExecute = true;
                }
            }
            // 备用：2D 射线检测 (以防项目里用的是 2D Collider)
            else
            {
                RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
                if (hit2D.collider != null)
                {
                    CharacterStats hitStats = hit2D.collider.GetComponentInParent<CharacterStats>();
                    if (hitStats != null && hitStats.gameObject.name != "Player")
                    {
                        targetEnemy = hitStats;
                        canExecute = true;
                    }
                }
            }
        }

        if(canExecute)
        {
            CharacterStats player = null;
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo != null) player = playerGo.GetComponent<CharacterStats>();

            // 寻找发牌器，以便移出卡牌并重新排布
            CardDeck deck = FindAnyObjectByType<CardDeck>();

            // 对于非攻击牌（如防御牌），不需要指向敌人，在场景中自动抓取除主角外的任何敌人作为结算目标
            if (!isAttack)
            {
                CharacterStats[] allStats = FindObjectsByType<CharacterStats>();
                foreach (var stat in allStats)
                {
                    if (stat.gameObject.name != "Player")
                    {
                        targetEnemy = stat;
                        break;
                    }
                }
            }

            if (player != null)
            {
                // 1. 尝试消耗灵气（能量）
                if (player.UseEnergy(currentCard.cardData.cost))
                {
                    // 2. 消耗成功，执行效果
                    CardManager.Instance.ExecuteCard(currentCard.cardData, player, targetEnemy);
                    
                    // 3. 从手牌列表移除并重新排序
                    if (deck != null) deck.RemoveCardFromHand(currentCard);
                    
                    // 4. 成功打出，销毁手牌物体
                    Destroy(gameObject);
                }
                else
                {
                    // 灵气不足，回弹卡牌
                    currentCard.ResetCardPosition();
                }
            }
            else
            {
                // 如果场景中没有Player物体（测试环境），则免消耗直接执行效果并销毁
                CardManager.Instance.ExecuteCard(currentCard.cardData, null, targetEnemy);
                if (deck != null) deck.RemoveCardFromHand(currentCard);
                Destroy(gameObject);
            }
        }
        else
        {
            currentCard.ResetCardPosition();
        }
    }
    
}
