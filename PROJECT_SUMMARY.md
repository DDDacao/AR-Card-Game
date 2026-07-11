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
| 符匣固定顺序 | ✅ | `FuXiaOrderSO`，不洗牌；开局 4 / 每回补 2 / 上限 6；奖励按关卡指定的基础抽牌节点插入；用后本场消耗 |
| 三色弱点 + QTE | ✅ | 红/黄/紫；命中匹配弱点 → 2 秒点 3 次；成功加伤/破甲/打断蓄力；QTE 中禁止继续出牌 |
| 敌人意图 | ✅ | 攻击→防御→蓄力（关卡可覆写）；只亮对应弱点 |
| 横屏 HUD | ✅ | 顶敌人、右上玩家、右中提示、右下结束回合；手牌 scale≈0.45 |
| 三关战役 | ✅ | 小妖→奖励→石灵→奖励→山鬼；失败重试本关 |
| 奖励三选一 | ✅ | `RewardSelectUI`；奖励按策划指定回合插入后续符匣，不再一律置顶 |
| 手机 AR 识别 | ✅ | 用户已验证扫图可用；Editor 用 `BattleBootstrap.skipARForEditor` 跳过 |
| 开始界面与开场动画 | ✅ | 场景保留唯一 `PF_StartIntro`；120 帧序列播放完成后才调用 `BattleBootstrap.BeginBattle()` 进入战役 |
| 灼烧构筑 | ✅ | 基础烈火符附加 1 层灼烧；奖励炼火符附加 2 层；敌方行动结束后每层扣 1 HP；奖励引火诀按每层 3 伤引爆并清空层数 |

**相对策划案仍缺**：精准 QTE、Boss 多阶段/独立模型、更丰富封印演出、真机「未识别不开战」门闩（可选）。

---

## 3. 战役与数据一览

### 3.1 流程

```
开始界面(PF_StartIntro，播放完毕)
  → BattleBootstrap.BeginBattle() → BattleFlowManager.StartCampaign()
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
| 基础牌 | `Card Data/attack(斩妖)` `defense(护身)` `break(破煞)` `fire(烈火)` `seal(镇魂)` `hp(聚气)` |
| 奖励牌 | `reward_lianzhan/lianhuo/zhenhunling/pozhen/yinhuo/dinghun` |
| 玩家 | `player.asset`（50HP / 3 灵气） |

### 3.3 已验证的固定补牌顺序（策划案 V1.0）

- **小妖**：开局 `斩、斩、护、聚`；第 2 回合 `斩、烈火`；第 3 回合 `护、斩`。
- **石灵**：开局 `破、斩、护、聚`；第 2 回合 `斩、烈火`；第 3 回合 `护、斩`；第 4 回合 `奖励 1、斩`。
- **山鬼**：开局 `镇、斩、护、奖励 1`；第 2 回合 `破、斩`；第 3 回合 `奖励 2、聚`；第 4 回合 `烈火、护`。

`CardDeck` 以“已抽基础牌数”为节点插入奖励，保证奖励卡不会错误地全塞到开局。

### 3.4 卡牌类型（`CardType`）

`Attack` | `Defense` | `Ability` | `ArmorBreak` | `Seal` | `Fire`

弱点标签 `WeaknessType`：`RedAttack` / `YellowArmor` / `PurpleSeal`  
（`CardDataSO.ResolveWeaknessTag()` 可按类型推断）

### 3.5 指向牌 vs 上拖牌

- **箭头指向**：Attack / ArmorBreak / Seal / Fire（`IsTargetedCard()`）
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

Assets/_ARSealCardGame/
  Prefabs/PF_StartIntro.prefab
  Scripts/UI/StartIntroController.cs  # 开始界面、帧序列与完成事件

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
8. **开始界面**：场景中只能保留一个 `PF_StartIntro`。`BattleBootstrap` 会自动订阅其 `IntroFinished` 事件；重复实例会导致动画需要点两次。
9. **灼烧结算**：当前在敌人行动完成后结算，每层 1 点伤害；引火诀会清空现有层数并按每层 3 点伤害引爆。
10. **策划卡牌分工**：烈火符是基础 10 牌中的火符；炼火符和引火诀分别是第一、第二次奖励池中的火符构筑牌。不要把三者合并或把引火诀当破甲牌。

---

## 8. 下一步必须做什么（按优先级）

### P0 — 下一条对话优先做（真机验收）

**A. 真机全流程回归 + 可选「识别后开战」**（约 0.5–1 天）

- [ ] 手机：扫图 → 三关 → 两次奖励 → 山鬼全通 / 失败重试  
- [ ] 可选：`BattleBootstrap` 真机关闭 `autoStartIfNoAR`，仅 `OnTargetFound` 时 `BeginBattle()`  
- [ ] 记：弱点位置、UI 安全区、结束回合与手牌是否挡操作  

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

**首选（发布/演示）：**

> 真机全流程回归：开始界面单次点击 → 开场动画 → 扫图 → 三关 → 两次奖励 → 山鬼全通 / 失败重试；记录弱点位置、UI 安全区、结束回合与手牌是否挡操作。

**次选（发布/演示）：**

> 真机门闩：开始界面完成且 ImageTarget 识别成功后才开战；Editor 保留 skipAR；避免开场动画结束时绕过识别门槛。

**再次选（表现）：**

> 出牌/QTE/死亡动画与简单特效，提升演示观感。

---

## 10. 快速自测清单（改完必过）

1. Play：显示一次开始界面；单次点击播放动画，结束后才进入第 1 关；开局 4 张（斩斩护聚一类教学序），符匣剩余正确。
2. 红/黄/紫意图切换时只有对应弱点亮。  
3. 指向匹配弱点 → QTE → 成功加伤/破甲/打断；QTE 未结束时不可继续出牌。
4. 击杀小妖 → 奖励三选一 → 石灵 32HP/有甲。  
5. 击杀石灵 → 再选奖励 → 山鬼 55HP。  
6. 失败 → 重试本关；全通 → 再来一局。  
7. 烈火符叠 1 层、炼火符叠 2 层灼烧；敌方行动后每层各扣 1 HP；引火诀每层引爆 3 点伤害并清空层数。
8. 手牌不过大、结束回合可点、中文不大量缺字。

---

## 11. 技术备忘

* Unity 6：查找用 `FindObjectsByType` / `FindAnyObjectByType`（无旧 API）。  
* Grok 若用 Unity MCP：`~/.grok/config.toml` 中 `[mcp_servers.unity-mcp]` → `http://localhost:8080/mcp`；Unity 需开 MCP Bridge。  
* 策划原文提取可用 pandoc/python 读 docx；本摘要已覆盖实现范围。  

---

**交接结论**：核心玩法闭环（开始界面 + 符匣 + 意图弱点 + QTE + 灼烧构筑 + 三关奖励）已可演示。新聊天请从 **§8 / §9** 选一条任务开干；优先 **真机全流程回归**，再做 **Boss 差异化**。
