using UnityEngine;

[CreateAssetMenu(fileName = "CardDataSO", menuName = "Card/CardDataSO")]
public class CardDataSO : ScriptableObject 
{
    public string cardName;

    public Sprite cardImage;

    public int cost;

    public CardType cardType;

    public string description;

    //todo: add card effect
}