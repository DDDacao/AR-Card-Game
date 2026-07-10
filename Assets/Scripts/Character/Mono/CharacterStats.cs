using UnityEngine;
using System;

public class CharacterStats : MonoBehaviour
{
    public CharacterDataSO templateData;

    [Header("运行时数据")]
    [SerializeField] private int currentHP;
    [SerializeField] private int currentEnergy;
    [SerializeField] private int currentArmor;

    // 事件通知，方便后续UI更新
    public event Action<int, int> OnHPChanged; // (current, max)
    public event Action<int, int> OnEnergyChanged; // (current, max)
    public event Action<int> OnArmorChanged; // (current)

    public int CurrentHP => currentHP;
    public int MaxHP => templateData != null ? templateData.maxHP : 0;
    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => templateData != null ? templateData.maxEnergy : 0;
    public int CurrentArmor => currentArmor;

    private void Start()
    {
        InitializeStats();
    }

    public void InitializeStats()
    {
        if (templateData != null)
        {
            currentHP = templateData.maxHP;
            currentEnergy = templateData.maxEnergy;
            currentArmor = templateData.startArmor;

            // 广播初始数值
            OnHPChanged?.Invoke(currentHP, templateData.maxHP);
            OnEnergyChanged?.Invoke(currentEnergy, templateData.maxEnergy);
            OnArmorChanged?.Invoke(currentArmor);
        }
        else
        {
            Debug.LogError($"{gameObject.name} 缺少 CharacterDataSO 模板数据！");
        }
    }

    #region HP/伤害管理
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        // 如果有护甲，优先扣除护甲
        if (currentArmor > 0)
        {
            if (currentArmor >= damage)
            {
                currentArmor -= damage;
                damage = 0;
            }
            else
            {
                damage -= currentArmor;
                currentArmor = 0;
            }
            OnArmorChanged?.Invoke(currentArmor);
        }

        // 剩余伤害扣除生命值
        if (damage > 0)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
            OnHPChanged?.Invoke(currentHP, MaxHP);

            if (currentHP == 0)
            {
                Die();
            }
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, MaxHP);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 死亡了！");
        // TODO: 触发死亡动画或结算面板
    }
    #endregion

    #region 护甲管理
    public void AddArmor(int amount)
    {
        if (amount <= 0) return;
        currentArmor += amount;
        OnArmorChanged?.Invoke(currentArmor);
    }

    public void ClearArmor()
    {
        currentArmor = 0;
        OnArmorChanged?.Invoke(currentArmor);
    }
    #endregion

    #region 能量/灵气管理
    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        currentEnergy = Mathf.Min(MaxEnergy, currentEnergy + amount);
        OnEnergyChanged?.Invoke(currentEnergy, MaxEnergy);
    }

    public bool UseEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            OnEnergyChanged?.Invoke(currentEnergy, MaxEnergy);
            return true;
        }
        else
        {
            Debug.LogWarning("灵气不足！");
            return false;
        }
    }

    public void ResetEnergy()
    {
        currentEnergy = MaxEnergy;
        OnEnergyChanged?.Invoke(currentEnergy, MaxEnergy);
    }
    #endregion
}
