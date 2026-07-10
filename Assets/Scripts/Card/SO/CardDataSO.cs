using UnityEngine;

[CreateAssetMenu(fileName = "CardDataSO", menuName = "Card/CardDataSO")]
public class CardDataSO : ScriptableObject 
{
    public string cardName;

    public Sprite cardImage;

    public int cost;

    public CardType cardType;

    public string description;

    [Header("效果数值 (如伤害、护甲值)")]
    public int effectValue;

    [Header("次要效果数值 (如附加灼烧层数、回灵气值)")]
    public int effectValue2;
}