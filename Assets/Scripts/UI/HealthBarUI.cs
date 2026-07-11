using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通用血条（敌人顶栏 / 可复用）
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    public CharacterStats characterStats;
    public bool isPlayer;

    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Image hpFill;

    public GameObject armorContainer;
    public TextMeshProUGUI armorText;

    private void Start()
    {
        Bind();
    }

    public void Bind()
    {
        if (characterStats == null)
        {
            if (isPlayer)
            {
                GameObject playerGo = GameObject.Find("Player");
                if (playerGo == null) playerGo = GameObject.Find("PlayerManager");
                if (playerGo != null) characterStats = playerGo.GetComponent<CharacterStats>();
            }
            else
            {
                CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var stat in allStats)
                {
                    if (stat.gameObject.name != "Player" && stat.gameObject.name != "PlayerManager")
                    {
                        characterStats = stat;
                        break;
                    }
                }
            }
        }

        if (characterStats == null) return;

        characterStats.OnHPChanged -= UpdateHPUI;
        characterStats.OnArmorChanged -= UpdateArmorUI;
        characterStats.OnHPChanged += UpdateHPUI;
        characterStats.OnArmorChanged += UpdateArmorUI;

        UpdateHPUI(characterStats.CurrentHP, characterStats.MaxHP);
        UpdateArmorUI(characterStats.CurrentArmor);
        ApplyDefaultColors();
    }

    public void Bind(CharacterStats stats)
    {
        if (characterStats != null)
        {
            characterStats.OnHPChanged -= UpdateHPUI;
            characterStats.OnArmorChanged -= UpdateArmorUI;
        }

        characterStats = stats;

        if (characterStats == null) return;

        characterStats.OnHPChanged -= UpdateHPUI;
        characterStats.OnArmorChanged -= UpdateArmorUI;
        characterStats.OnHPChanged += UpdateHPUI;
        characterStats.OnArmorChanged += UpdateArmorUI;

        UpdateHPUI(characterStats.CurrentHP, characterStats.MaxHP);
        UpdateArmorUI(characterStats.CurrentArmor);
        ApplyDefaultColors();
    }

    private void ApplyDefaultColors()
    {
        Color red = new Color(0.78f, 0.12f, 0.12f, 1f);
        Color bg = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        if (hpFill != null)
            hpFill.color = red;
        else if (hpSlider != null && hpSlider.fillRect != null)
        {
            Image fillImage = hpSlider.fillRect.GetComponent<Image>();
            if (fillImage != null) fillImage.color = red;
        }

        if (hpSlider != null)
        {
            Transform bgTransform = hpSlider.transform.Find("Background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null) bgImage.color = bg;
            }
        }
    }

    private void OnDestroy()
    {
        if (characterStats != null)
        {
            characterStats.OnHPChanged -= UpdateHPUI;
            characterStats.OnArmorChanged -= UpdateArmorUI;
        }
    }

    private void UpdateHPUI(int currentHP, int maxHP)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = Mathf.Max(1, maxHP);
            hpSlider.value = currentHP;
        }
        if (hpText != null)
            hpText.text = $"{currentHP}/{maxHP}";
    }

    private void UpdateArmorUI(int currentArmor)
    {
        if (armorContainer != null)
            armorContainer.SetActive(currentArmor > 0);
        if (armorText != null)
            armorText.text = currentArmor.ToString();
    }
}
