using System.Collections.Generic;
using UnityEngine;

public class CardLayoutManager : MonoBehaviour
{
    public bool isHorizontal;
    [Tooltip("手牌扇区最大宽度（世界单位，配合更小卡牌可略收紧）")]
    public float maxWidth = 5.5f;
    [Tooltip("卡牌基础间距")]
    public float cardSpacing = 1.55f;
    public Vector3 centerPoint;

    [SerializeField] private List<Vector3> cardPositions = new();
    private List<Quaternion> cardRotations = new();

    public CardTransform GetCardTransform(int index, int totalCards)
{
    CalculatePosition(totalCards, isHorizontal);

    return new CardTransform(cardPositions[index], cardRotations[index]);
}

private void CalculatePosition(int numberOfCards, bool horizontal)
{
    cardPositions.Clear();
    cardRotations.Clear();

    if (horizontal)
    {
        float currentWidth = cardSpacing * (numberOfCards - 1);
        float totalWidth = Mathf.Min(currentWidth, maxWidth);

        float currentSpacing = totalWidth > 0 ? totalWidth / (numberOfCards - 1) : 0;

        for (int i = 0; i < numberOfCards; i++)
        {
            float xPos = 0 - (totalWidth / 2) + (i * currentSpacing);

            
            var pos = new Vector3(xPos, centerPoint.y, 0f);
            var rotation = Quaternion.identity;

            cardPositions.Add(pos);
            cardRotations.Add(rotation);
        }
    }
}
}