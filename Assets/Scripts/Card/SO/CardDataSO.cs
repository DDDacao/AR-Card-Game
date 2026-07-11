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

    [Header("弱点标签（None 时按卡牌类型推断）")]
    public WeaknessType weaknessTag = WeaknessType.None;

    /// <summary>
    /// 解析本卡对应的弱点类型
    /// </summary>
    public WeaknessType ResolveWeaknessTag()
    {
        if (weaknessTag != WeaknessType.None)
            return weaknessTag;

        switch (cardType)
        {
            case CardType.Attack: return WeaknessType.RedAttack;
            case CardType.ArmorBreak: return WeaknessType.YellowArmor;
            case CardType.Seal: return WeaknessType.PurpleSeal;
            default: return WeaknessType.None;
        }
    }

    public bool IsTargetedCard()
    {
        return cardType == CardType.Attack
            || cardType == CardType.ArmorBreak
            || cardType == CardType.Seal;
    }
}
