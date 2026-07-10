using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDataSO", menuName = "Character/CharacterDataSO")]
public class CharacterDataSO : ScriptableObject
{
    [Header("角色名")]
    public string characterName;

    [Header("生命值")]
    public int maxHP;

    [Header("能量/灵气值")]
    public int maxEnergy;

    [Header("初始护甲")]
    public int startArmor;
}
