using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public static bool InteractionEnabled = true;

    public GameObject arrowPrefab;
    private GameObject currentArrow;

    private Card currentCard;
    private bool canMove;
    private bool canExecute;

    public bool IsDragging { get; private set; }

    private WeaknessPoint aimedWeakness;
    private bool isTargetedDrag;

    private void Awake()
    {
        currentCard = GetComponent<Card>();
    }

    private void OnDisable()
    {
        ClearWeaknessAim();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ClearWeaknessAim();

        if (!InteractionEnabled)
        {
            Debug.LogWarning("[CardDragHandler] AR 识别丢失，手牌交互已禁用。");
            return;
        }

        if (QTEManager.Instance != null && QTEManager.Instance.IsRunning)

        {
            Debug.LogWarning("[CardDragHandler] QTE 进行中，不能出牌！");
            return;
        }

        if (TurnManager.Instance != null)
        {
            if (!TurnManager.Instance.IsBattleActive || TurnManager.Instance.BattleEnded)
            {
                Debug.LogWarning("[CardDragHandler] 战斗未开始或已结束，不能出牌！");
                return;
            }
            if (!TurnManager.Instance.IsPlayerTurn)
            {
                Debug.LogWarning("[CardDragHandler] 现在是怪物回合，不能出牌！");
                return;
            }
        }

        if (currentCard == null || currentCard.cardData == null) return;

        IsDragging = true;
        Transform visual = transform.Find("Entry");
        if (visual != null)
        {
            visual.DOKill();
            visual.localPosition = Vector3.zero;
        }

        isTargetedDrag = currentCard.cardData.IsTargetedCard();
        if (isTargetedDrag)
        {
            currentArrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            int cardLayer = LayerMask.NameToLayer("Card");
            if (cardLayer == -1) cardLayer = 6;
            CardCameraManager.SetLayerRecursive(currentArrow, cardLayer);
        }
        else
        {
            // 防御 / 技能：上拖打出
            canMove = true;
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
        // 指向牌：拖动时更新弱点瞄准高亮（不移动手牌位置）
        if (isTargetedDrag && IsDragging)
        {
            UpdateWeaknessAim(Input.mousePosition);
            return;
        }

        if (!canMove) return;

        Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
        Camera dragCamera = (CardCameraManager.Instance != null && CardCameraManager.Instance.cardCamera != null)
            ? CardCameraManager.Instance.cardCamera
            : Camera.main;
        if (dragCamera == null) return;

        Vector3 worldPos = dragCamera.ScreenToWorldPoint(screenPos);
        currentCard.transform.position = worldPos;
        canExecute = worldPos.y > 0.5f;
    }

    private void Update()
    {
        // 部分平台 OnDrag 频率低，指向拖动时每帧补一次瞄准
        if (IsDragging && isTargetedDrag)
            UpdateWeaknessAim(Input.mousePosition);
    }

    private void UpdateWeaknessAim(Vector3 screenPos)
    {
        ResolveAttackTarget(screenPos, out _, out var wp, out _);
        bool match = false;
        if (wp != null && currentCard != null && currentCard.cardData != null)
        {
            var tag = currentCard.cardData.ResolveWeaknessTag();
            match = tag != WeaknessType.None && wp.weaknessType == tag && wp.gameObject.activeInHierarchy;
        }

        if (aimedWeakness != null && aimedWeakness != wp)
            aimedWeakness.SetAimed(false, false);

        aimedWeakness = wp;
        if (aimedWeakness != null && aimedWeakness.gameObject.activeInHierarchy)
            aimedWeakness.SetAimed(true, match);
        else if (aimedWeakness != null)
            aimedWeakness.SetAimed(false, false);
    }

    private void ClearWeaknessAim()
    {
        if (aimedWeakness != null)
        {
            aimedWeakness.SetAimed(false, false);
            aimedWeakness = null;
        }
        isTargetedDrag = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        ClearWeaknessAim();

        if (currentArrow != null)
        {
            Destroy(currentArrow);
            currentArrow = null;
        }

        if (currentCard == null || currentCard.cardData == null)
        {
            canMove = false;
            canExecute = false;
            return;
        }

        bool isTargeted = currentCard.cardData.IsTargetedCard();
        CharacterStats targetEnemy = null;
        WeaknessPoint hitWeakness = null;

        if (isTargeted)
        {
            // 使用全部命中结果：身体 BoxCollider 常挡在弱点前面，单次 Raycast 会漏掉弱点
            ResolveAttackTarget(Input.mousePosition, out targetEnemy, out hitWeakness, out canExecute);
        }

        if (canExecute)
        {
            CharacterStats player = null;
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo == null) playerGo = GameObject.Find("PlayerManager");
            if (playerGo != null) player = playerGo.GetComponent<CharacterStats>();

            CardDeck deck = FindAnyObjectByType<CardDeck>();

            if (!isTargeted)
            {
                CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var stat in allStats)
                {
                    if (stat.gameObject.name != "Player" && stat.gameObject.name != "PlayerManager")
                    {
                        targetEnemy = stat;
                        break;
                    }
                }
            }

            bool hitMatchedWeakness = false;
            if (isTargeted && hitWeakness != null && currentCard.cardData != null)
            {
                var tag = currentCard.cardData.ResolveWeaknessTag();
                hitMatchedWeakness = tag != WeaknessType.None && hitWeakness.weaknessType == tag;
            }

            if (player != null)
            {
                if (player.UseEnergy(currentCard.cardData.cost))
                {
                    CardManager.Instance.ExecuteCard(
                        currentCard.cardData,
                        player,
                        targetEnemy,
                        hitMatchedWeakness,
                        hitWeakness);

                    if (deck != null) deck.RemoveCardFromHand(currentCard);
                    Destroy(gameObject);
                }
                else
                {
                    currentCard.ResetCardPosition();
                }
            }
            else
            {
                CardManager.Instance.ExecuteCard(
                    currentCard.cardData,
                    null,
                    targetEnemy,
                    hitMatchedWeakness,
                    hitWeakness);
                if (deck != null) deck.RemoveCardFromHand(currentCard);
                Destroy(gameObject);
            }
        }
        else
        {
            currentCard.ResetCardPosition();
        }

        canMove = false;
        canExecute = false;
    }

    /// <summary>
    /// 从屏幕点发射 3D 射线，优先解析弱点点，其次敌人身体。
    /// </summary>
    private static void ResolveAttackTarget(Vector3 screenPos, out CharacterStats enemy, out WeaknessPoint weakness, out bool valid)
    {
        enemy = null;
        weakness = null;
        valid = false;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            // 2D 备用
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
            {
                weakness = hit2D.collider.GetComponent<WeaknessPoint>();
                if (weakness == null)
                    weakness = hit2D.collider.GetComponentInParent<WeaknessPoint>();
                enemy = hit2D.collider.GetComponentInParent<CharacterStats>();
                if (IsEnemyStats(enemy) || weakness != null)
                {
                    if (weakness != null && weakness.GetOwner() != null)
                        enemy = weakness.GetOwner();
                    valid = enemy != null;
                }
            }
            return;
        }

        // 由近到远
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // 1) 优先任意命中上的弱点（即使被身体挡住，也只要射线穿过弱点体积）
        for (int i = 0; i < hits.Length; i++)
        {
            var wp = hits[i].collider.GetComponent<WeaknessPoint>();
            if (wp == null)
                wp = hits[i].collider.GetComponentInParent<WeaknessPoint>();
            if (wp != null)
            {
                weakness = wp;
                enemy = wp.GetOwner();
                if (IsEnemyStats(enemy))
                {
                    valid = true;
                    return;
                }
            }
        }

        // 2) 否则取第一个敌人身体
        for (int i = 0; i < hits.Length; i++)
        {
            var stats = hits[i].collider.GetComponentInParent<CharacterStats>();
            if (IsEnemyStats(stats))
            {
                enemy = stats;
                valid = true;
                return;
            }
        }
    }

    private static bool IsEnemyStats(CharacterStats stats)
    {
        if (stats == null) return false;
        string n = stats.gameObject.name;
        return n != "Player" && n != "PlayerManager";
    }
}
