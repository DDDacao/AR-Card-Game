using UnityEngine;

public enum CardType
{
    Attack,      // 攻击 — 红弱点
    Defense,     // 防御
    Ability,     // 技能
    ArmorBreak,  // 破甲 — 黄弱点
    Seal,        // 镇魂 — 紫弱点
    Fire         // 火符 — 命中敌人后附加或引爆灼烧
}

/// <summary>
/// 卡牌在基础类型结算之外附带的状态效果。
/// </summary>
public enum CardSpecialEffect
{
    None = 0,
    ApplyBurn = 1,
    DetonateBurn = 2
}

/// <summary>
/// 弱点 / 符咒标签
/// </summary>
public enum WeaknessType
{
    None = 0,
    RedAttack = 1,   // 红色破绽 — 攻击符
    YellowArmor = 2, // 黄色裂纹 — 破甲符
    PurpleSeal = 3   // 紫色封印 — 镇魂符
}

/// <summary>
/// 敌人意图（决定本回合暴露的弱点与敌方行动）。
/// 弱点颜色由 IntentStep.exposedWeakness 配置，可为 None（本回合无弱点）。
/// </summary>
public enum EnemyIntentKind
{
    Attack,  // 攻击：造成 power 点伤害
    Defend,  // 防御：获得 armorGain 点护甲
    Charge,  // 蓄力蓄势：本步不造成伤害；可被镇魂打断；打断失败则进入 IsCharging
    Heavy    // 蓄力释放：仅当上一蓄力未被打断时造成 power 点伤害
}
