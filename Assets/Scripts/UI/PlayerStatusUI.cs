using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 参考图右上：玩家生命 / 护甲 / 灵气（横屏）
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    public CharacterStats characterStats;
    public bool autoFindPlayer = true;

    [Header("生命")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Image hpFill;

    [Header("护甲")]
    public GameObject armorContainer;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI armorLabel;

    [Header("灵气")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI energyLabel;

    private void Start()
    {
        Bind();
    }

    public void Bind()
    {
        if (characterStats == null && autoFindPlayer)
        {
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo == null) playerGo = GameObject.Find("PlayerManager");
            if (playerGo != null) characterStats = playerGo.GetComponent<CharacterStats>();
        }

        if (characterStats == null) return;

        characterStats.OnHPChanged -= UpdateHP;
        characterStats.OnArmorChanged -= UpdateArmor;
        characterStats.OnEnergyChanged -= UpdateEnergy;

        characterStats.OnHPChanged += UpdateHP;
        characterStats.OnArmorChanged += UpdateArmor;
        characterStats.OnEnergyChanged += UpdateEnergy;

        UpdateHP(characterStats.CurrentHP, characterStats.MaxHP);
        UpdateArmor(characterStats.CurrentArmor);
        UpdateEnergy(characterStats.CurrentEnergy, characterStats.MaxEnergy);
        ApplyColors();
    }

    private void OnDestroy()
    {
        if (characterStats != null)
        {
            characterStats.OnHPChanged -= UpdateHP;
            characterStats.OnArmorChanged -= UpdateArmor;
            characterStats.OnEnergyChanged -= UpdateEnergy;
        }
    }

    private void ApplyColors()
    {
        if (hpFill != null)
            hpFill.color = new Color(0.78f, 0.12f, 0.12f, 1f);
        else if (hpSlider != null && hpSlider.fillRect != null)
        {
            var img = hpSlider.fillRect.GetComponent<Image>();
            if (img != null) img.color = new Color(0.78f, 0.12f, 0.12f, 1f);
        }
    }

    private void UpdateHP(int current, int max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = Mathf.Max(1, max);
            hpSlider.value = current;
        }
        if (hpText != null)
            hpText.text = $"{current}/{max}";
    }

    private void UpdateArmor(int armor)
    {
        if (armorContainer != null)
            armorContainer.SetActive(true); // 始终显示行，0 也显示
        if (armorText != null)
            armorText.text = armor.ToString();
    }

    private void UpdateEnergy(int current, int max)
    {
        if (energyText != null)
            energyText.text = $"{current}/{max}";
    }
}
