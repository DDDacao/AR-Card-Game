# AR封妖牌局 — PROJECT_SUMMARY（交接用）

> **用途**：新对话 / 新同学接入时先读本文件。  
> **更新日期**：2026-07-11  
> **策划案**：根目录 `AR封妖牌局_第一版玩法策划案.docx`  
> **UI 参考**：根目录 `UI设计参考图.png`（横屏简易 HUD）

---

## 0. 新对话怎么开（复制即可）

在新聊天中可以说：

```text
请先读 F:\AR-Card-Game\PROJECT_SUMMARY.md，从「## 下一步必须做什么」开始继续开发。
工程是 Unity 6 + Vuforia AR 卡牌。场景 SampleScene。可用菜单 AR封妖/* 一键重配。
```

若要做某一具体项，直接点名，例如：

- 「做灼烧 DoT 和烈火符」
- 「真机改为识别成功后再开战」
- 「Boss 山鬼换模型和多阶段」

---

## 1. 项目一句话

玩家在现实桌面扫图召唤妖怪，通过 **保留符咒、观察意图/弱点、打出对应符、完成 QTE** 进行回合制封妖。  
技术栈：**Unity 6 URP + Vuforia + Addressables + DOTween + TMP**。  
主场景：`Assets/Scenes/SampleScene.unity`。

---

## 2. 当前可玩进度（已实现）

| 模块 | 状态 | 说明 |
|------|------|------|
| 双相机手牌 | ✅ | ARCamera Base + CardCamera Overlay，`Card` 层 |
| 出牌交互 | ✅ | 指向牌箭头+射线；防/技能上拖；灵气不足回弹 |
| 符匣固定顺序 | ✅ | `FuXiaOrderSO`，不洗牌；开局 4 / 每回补 2 / 上限 6；用后本场消耗 |
| 三色弱点 + QTE | ✅ | 红/黄/紫；命中匹配弱点 → 2 秒点 3 次；成功加伤/破甲/打断蓄力 |
| 敌人意图 | ✅ | 攻击→防御→蓄力（关卡可覆写）；只亮对应弱点 |
| 横屏 HUD | ✅ | 顶敌人、右上玩家、右中提示、右下结束回合；手牌 scale≈0.45 |
| 三关战役 | ✅ | 小妖→奖励→石灵→奖励→山鬼；失败重试本关 |
| 奖励三选一 | ✅ | `RewardSelectUI`；奖励插入后续符匣头部 |
| 手机 AR 识别 | ✅ | 用户已验证扫图可用；Editor 用 `BattleBootstrap.skipARForEditor` 跳过 |

**相对策划案仍缺**：烈火/灼烧完整 Build、精准 QTE、Boss 多阶段/独立模型、更丰富封印演出、真机「未识别不开战」门闩（可选）。

---

## 3. 战役与数据一览

### 3.1 流程

```
开战(BattleBootstrap → BattleFlowManager.StartCampaign)
  → 第1关 小妖(24HP) → 奖励三选一
  → 第2关 石灵(32HP/开局6甲) → 奖励三选一
  → 第3关 山鬼(55HP) → 全通关
失败 → 重试本关
```

### 3.2 关键资产路径

| 类型 | 路径 |
|------|------|
| 战役 | `Assets/Game Data/Stages/Campaign_Main.asset` |
| 关卡 | `Stage_01_XiaoYao` / `Stage_02_ShiLing` / `Stage_03_ShanGui` |
| 敌人数值 | `enemy_xiaoyao` / `enemy_shiling` / `enemy_shangui` |
| 符匣 | `Card Library/FuXia_XiaoYao|ShiLing|ShanGui.asset` |
| 基础牌 | `Card Data/attack(斩妖)` `defense(护身)` `break(破煞)` `seal(镇魂)` `hp(聚气)` |
| 奖励牌 | `reward_lianzhan/lianhuo/zhenhunling/pozhen/yinhuo/dinghun` |
| 玩家 | `player.asset`（50HP / 3 灵气） |

### 3.3 卡牌类型（`CardType`）

`Attack` | `Defense` | `Ability` | `ArmorBreak` | `Seal`  

弱点标签 `WeaknessType`：`RedAttack` / `YellowArmor` / `PurpleSeal`  
（`CardDataSO.ResolveWeaknessTag()` 可按类型推断）

### 3.4 指向牌 vs 上拖牌

- **箭头指向**：Attack / ArmorBreak / Seal（`IsTargetedCard()`）
- **上拖打出**：Defense / Ability

---

## 4. 运行时架构（谁调用谁）

```
BattleBootstrap.BeginBattle()
  └─ BattleFlowManager.StartCampaign()
       └─ ApplyStageToBattle(stage)  // 敌人SO、符匣、意图、显示名
       └─ TurnManager.StartBattle()  // 抽开局手牌、展示意图/弱点

玩家出牌 CardDragHandler
  └─ RaycastAll 优先 WeaknessPoint
  └─ CardManager.ExecuteCard(..., hitWeakness, wp)
       └─ 匹配弱点 → QTEManager → 结算伤害/破甲/打断

结束回合 TurnManager.EndPlayerTurn
  └─ EnemyIntentController.ExecuteAndAdvance(player)
  └─ 下一玩家回合 PresentIntent（换弱点）

胜负 TurnManager.OnBattleEnded
  └─ BattleFlowManager：奖励 UI / 下一关 / 全通 / 重试
```

---

## 5. 关键脚本地图

```
Assets/Scripts/
  Campaign/
    BattleFlowManager.cs   # 三关 + 奖励流转
    BattleStageSO.cs
    CampaignSO.cs
  Card/
    Mono/ Card.cs, CardDeck.cs, CardDragHandler.cs, CardArrow.cs
    SO/   CardDataSO.cs, CardLibrarySO.cs, FuXiaOrderSO.cs
  Character/
    Mono/ CharacterStats.cs, WeaknessPoint.cs, EnemyIntentController.cs
    SO/   CharacterDataSO.cs
  Combat/
    QTEManager.cs
  Managers/
    TurnManager.cs, CardManager.cs, BattleBootstrap.cs
    CardCameraManager.cs, CardLayoutManager.cs
  UI/
    PlayerStatusUI, BattleInfoUI, BattleResultUI, HealthBarUI
    QTEPanelUI, RewardSelectUI
  Utilities/
    Enums.cs, PoolTool.cs, CardTranfrom.cs
  EllenARController.cs     # 动画按钮测试，非核心战斗

Assets/Editor/             # 一键配置菜单（见下）
```

---

## 6. Unity 编辑器菜单（AR封妖）

| 菜单 | 作用 |
|------|------|
| `AR封妖/重建战斗HUD（横屏）` | 按参考图重建 Canvas HUD |
| `AR封妖/配置弱点与QTE` | 红弱点 + QTE 面板 + QTEManager |
| `AR封妖/配置多弱点与新符咒` | 三色弱点 + 破煞/镇魂 + 牌库数量 |
| `AR封妖/配置符匣固定顺序` | 生成/绑定 `FuXia_XiaoYao` |
| `AR封妖/配置三关战役` | 战役 SO + 三关符匣/奖励 + BattleFlowManager + 奖励 UI |

**场景里应有**：`ARCamera`、`ImageTarget`、`Ellen_skin (2)`（含 CharacterStats + 三弱点 + EnemyIntentController）、`CardManager`/`CardDeck`/`TurnManager`、`BattleBootstrap`、`BattleFlowManager`、`Canvas`（含 HUD_* / HUD_QTE / HUD_Reward）、`QTEManager`。

---

## 7. 已知问题 / 注意点

1. **弱点射线**：必须用 `RaycastAll` 并优先 `WeaknessPoint`（身体 BoxCollider 会挡单次 Raycast）——已在 `CardDragHandler` 处理。  
2. **QTE 时**：禁止出牌、禁止结束回合。  
3. **胜负 UI**：有 `BattleFlowManager` 时由流程接管，不要只依赖默认「再战一次」。  
4. **奖励 UI**：`RewardSelectUI` 与 `HUD_Reward` 需激活且置顶；`BattleFlowManager.Awake` 里 Subscribe 避免漏事件。  
5. **手牌大小**：`CardDeck.handCardScale` 当前约 **0.45**，可在 Inspector 调。  
6. **Editor Vuforia**：Webcam / stream 警告可忽略；真机识别用户已确认 OK。  
7. **敌人模型**：三关仍共用 Ellen 占位模型，靠 `CharacterDataSO` 改血甲与意图区分。  
8. **炼火/引火**：奖励里多为占位描述，**灼烧层数 DoT 尚未实现**。

---

## 8. 下一步必须做什么（按优先级）

### P0 — 建议下一条对话优先做（可玩性/完成度）

**A. 真机全流程回归 + 可选「识别后开战」**（约 0.5–1 天）

- [ ] 手机：扫图 → 三关 → 两次奖励 → 山鬼全通 / 失败重试  
- [ ] 可选：`BattleBootstrap` 真机关闭 `autoStartIfNoAR`，仅 `OnTargetFound` 时 `BeginBattle()`  
- [ ] 记：弱点位置、UI 安全区、结束回合与手牌是否挡操作  

**B. 灼烧 DoT + 烈火/引火真正生效**（约 1–2 天）— **策划 Build 缺口最大**

- [ ] `CharacterStats`：灼烧层数、回合结束结算伤害  
- [ ] `CardManager`：烈火叠层、引火引爆（奖励卡 `reward_lianhuo` / `reward_yinhuo` 接真实逻辑）  
- [ ] 意图/UI 可显示当前灼烧层数  

### P1 — 内容与表现

**C. Boss 差异化**

- [ ] 山鬼独立模型或缩放/材质区分  
- [ ] 蓄力更狠的演出；打断成功反馈  
- [ ] （可选）Boss 第二阶段意图表  

**D. 动画与反馈**

- [ ] 受击/死亡接 `EllenARController` 或 Animator  
- [ ] QTE 成功/失败特效与音效  
- [ ] 弱点点更明显（描边/脉冲）  

### P2 — 策划案剩余

- [ ] 精准点击 QTE（圆环）  
- [ ] 更完整基础 10 牌数值与三关符匣手感调参  
- [ ] 连斩「本回合已出过攻击则额外伤害」条件效果  

---

## 9. 建议的下一条任务（直接开干用）

**首选（内容）：**

> 实现灼烧状态：回合结束按层扣血；烈火符叠层；引火诀引爆；在敌人状态或提示区显示灼烧层数。接上已有 reward_lianhuo / reward_yinhuo。

**次选（发布/演示）：**

> 真机门闩：仅 ImageTarget 识别成功后开战；Editor 保留 skipAR；并列出三关回归检查清单。

**再次选（表现）：**

> 出牌/QTE/死亡动画与简单特效，提升演示观感。

---

## 10. 快速自测清单（改完必过）

1. Play：自动进第 1 关，开局 4 张（斩斩护聚一类教学序），符匣剩余正确。  
2. 红/黄/紫意图切换时只有对应弱点亮。  
3. 指向匹配弱点 → QTE → 成功加伤/破甲/打断。  
4. 击杀小妖 → 奖励三选一 → 石灵 32HP/有甲。  
5. 击杀石灵 → 再选奖励 → 山鬼 55HP。  
6. 失败 → 重试本关；全通 → 再来一局。  
7. 手牌不过大、结束回合可点、中文不大量缺字。  

---

## 11. 技术备忘

* Unity 6：查找用 `FindObjectsByType` / `FindAnyObjectByType`（无旧 API）。  
* Grok 若用 Unity MCP：`~/.grok/config.toml` 中 `[mcp_servers.unity-mcp]` → `http://localhost:8080/mcp`；Unity 需开 MCP Bridge。  
* 策划原文提取可用 pandoc/python 读 docx；本摘要已覆盖实现范围。  

---

**交接结论**：核心玩法闭环（符匣 + 意图弱点 + QTE + 三关奖励）已可演示。新聊天请从 **§8 / §9** 选一条任务开干；优先 **灼烧卡效** 或 **真机全流程回归**。
