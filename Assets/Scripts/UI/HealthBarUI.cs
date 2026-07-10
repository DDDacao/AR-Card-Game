using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("关联的角色属性")]
    public CharacterStats characterStats;

    [Header("是否自动关联场景中的主角")]
    public bool isPlayer;

    [Header("UI 元素")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    
    [Header("护甲 UI (可选)")]
    public GameObject armorContainer; // 包含护甲图标和数值的父物体，没护甲时隐藏
    public TextMeshProUGUI armorText;

    private void Start()
    {
        // 自动寻路关联数据源
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
                // 寻找非 Player 的 CharacterStats 作为敌人
                CharacterStats[] allStats = FindObjectsByType<CharacterStats>();
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

        if (characterStats != null)
        {
            // 绑定事件监听
            characterStats.OnHPChanged += UpdateHPUI;
            characterStats.OnArmorChanged += UpdateArmorUI;

            // 初始化界面显示
            UpdateHPUI(characterStats.CurrentHP, characterStats.MaxHP);
            UpdateArmorUI(characterStats.CurrentArmor);

            // 自动帮你调整血条颜色：Fill 设为红色，Background 设为暗灰色
            ApplyDefaultColors();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 无法关联到对应的 CharacterStats，请检查场景或手动拖拽赋值！");
        }
    }

    private void ApplyDefaultColors()
    {
        if (hpSlider != null)
        {
            // 1. 自动调整填充物 (Fill) 的颜色为鲜红色
            if (hpSlider.fillRect != null)
            {
                Image fillImage = hpSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = new Color(0.85f, 0.1f, 0.1f, 1f); // 红色
                }
            }

            // 2. 自动调整底面背景 (Background) 的颜色为暗灰色
            Transform bgTransform = hpSlider.transform.Find("Background");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(0.18f, 0.18f, 0.18f, 1f); // 暗灰色
                }
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
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }
    }

    private void UpdateArmorUI(int currentArmor)
    {
        if (armorContainer != null)
        {
            // 如果护甲为0，隐藏整个护甲图标容器
            armorContainer.SetActive(currentArmor > 0);
        }

        if (armorText != null)
        {
            armorText.text = currentArmor.ToString();
        }
    }
}
