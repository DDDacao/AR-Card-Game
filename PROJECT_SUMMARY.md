# AR封妖牌局 — PROJECT_SUMMARY（交接用）

> **用途**：新对话 / 新同学 / 新 agent 接入时先读本文件。  
> **更新日期**：2026-07-12（新版战斗 HUD + 三关战役完全解耦 + 模拟扫卡及弱点已修复并验收）  
> **策划案**：根目录 `AR封妖牌局_第一版玩法策划案.docx`  
> **UI 参考**：根目录 `UI设计参考图.png`  
> **铃铛图**：`Assets/_ARSealCardGame/Art/UI/QTE/bell.png`  
> **卡面零件**：`Assets/Prefabs/Cardpng/`（底/圆框/费用角/名称条 + 各符图案）  
> **手牌预制体**：`Assets/Prefabs/Card/Card.prefab`（**卡面已拼好**；数据在 SO）

---

## 0. 新对话怎么开（复制即可）

### 0.1 通用（推荐）

```text
请先读 F:\AR-Card-Game\PROJECT_SUMMARY.md，重点看 §2 进度、§8 下一步、§15 新版 UI、§16 奖励与卡牌结构。
工程是 Unity 6 URP + Vuforia AR。主场景 Assets/Scenes/SampleScene.unity。
当前三关解耦战役已完全打通，编辑器模拟与真机扫卡均运行良好。
下一步任务优先：真机全流程物理扫图测试，或接入震动/音效与受击动画节奏微调。
```

### 0.2 其它任务示例

- 「真机物理扫卡全流程测试并产出问题清单」
- 「UI安全区在真机界面的适配」
- 「玩家受击加手机震动 / QTE 音效」

---

## 1. 项目一句话

玩家在现实桌面扫图召唤妖怪，通过 **保留符咒、观察意图/弱点、打出对应符、完成 QTE** 进行回合制封妖。  
技术栈：**Unity 6 URP + Vuforia + Addressables + DOTween + TMP**。  
主场景：`Assets/Scenes/SampleScene.unity`。

---

## 2. 当前可玩进度（已实现）

| 模块 | 状态 | 说明 |
|------|------|------|
| 双相机手牌 | ✅ | ARCamera Base + CardCamera Overlay，`Card` 层（支持开战时动态堆栈刷新，防止 Vuforia 覆盖） |
| 出牌交互 | ✅ | 指向牌箭头+射线；防/技能上拖；灵气不足回弹 |
| 符匣固定顺序 | ✅ | 策划案 V1.0；菜单 `AR封妖/按策划重置符匣与火符` |
| 卡牌数据/命名 | ✅ | SO 英文 id + 中文 `cardName`；运行时 `Card_斩妖符` |
| 三色弱点 + QTE | ✅ | 逻辑正常；已永久精确挂接至三关怪物头部骨骼；无粒子方块 |
| 镇魂铃 QTE | ✅ | 全屏压暗 + 大铃；2 秒点 3 次；成功后 UI 关再结算 |
| 伤害跳字 / 闪色 / 受击 | ✅ | `DamagePopup` / `MonsterHitFlash` / `PlayerDamageFeedback` |
| 敌人意图 | ✅ | 攻击→防御→蓄力；只亮对应弱点 |
| **新版战斗 HUD** | ✅ | `HUD_ArtSkin_Adjustable` 可手调；用户已验收整体 |
| **意图文案** | ✅ | **仅一行**意图名（如「普通攻击」）；见 §15.2 |
| **回合状态** | ✅ | 第 N 回合 / 玩家或妖怪阶段 / 牌堆剩余与手牌；中文字体 `2` |
| **奖励三选一** | ✅ | **实例化真实 `Card.prefab`**，不 UI 重拼；见 §16 |
| 三关战役 + 换模 | ✅ | 完全解耦为 3 个独立 ImageTarget 扫卡触发对战，不依赖旧 Ellen；支持线性关卡解锁与通关逻辑 |
| 灼烧构筑 | ✅ | 烈火 1 / 炼火 2；敌方行动后结算；引火引爆 |
| 开始界面 | ✅ | 唯一 `PF_StartIntro` |
| 手机 AR / 模拟 | ✅ | 真机 Vuforia 扫卡与 Editor 模拟控制台功能完备；已解决模拟不显模型和手牌隐藏 Bug |

**相对策划案仍缺**：精准圆环 QTE、Boss 多阶段、更丰富封印演出、受击音效/震动、真机全流程回归。

---

## 3. 战役与数据一览

### 3.1 流程

```
开始界面(PF_StartIntro)
  → BattleBootstrap.BeginBattle() → BattleFlowManager.StartCampaign()
  → 第1关 小妖(24HP) Vespomorph → 奖励三选一
  → 第2关 石灵(32HP/开局6甲) Cavecrawler → 奖励三选一
  → 第3关 山鬼(55HP) Drackmahre → 全通关
失败 → 重试本关
```

### 3.2 关键资产路径

| 类型 | 路径 |
|------|------|
| 战役 | `Assets/Game Data/Stages/Campaign_Main.asset` |
| 关卡 | `Stage_01_XiaoYao` / `Stage_02_ShiLing` / `Stage_03_ShanGui` |
| 敌人 | `enemy_xiaoyao` / `enemy_shiling` / `enemy_shangui` |
| 符匣 | `Assets/Game Data/Card Library/FuXia_*.asset` |
| 卡牌数据 SO | `Assets/Game Data/Card Data/`（`attack`/`defense`/`break`/…/`reward_*`） |
| 手牌预制体 | `Assets/Prefabs/Card/Card.prefab` |
| 卡面零件+图案 | `Assets/Prefabs/Cardpng/` |
| 怪物 Prefab | `Assets/fbx/monsters/1|2|3/Prefabs/` |
| 中文字体 | `Assets/Scripts/Utilities/2.asset`、`3.asset`、`ziti.asset`（优先 2/3） |
| HUD 美术 | `Assets/Resources/BattleHudSkin/` |

### 3.3 固定补牌顺序（策划案 V1.0）

- **小妖**：开局 `斩斩护聚`；第 2 回合 `斩烈火`；第 3 回合 `护斩`（尾部填充破/镇）。  
- **石灵**：开局 `破斩护聚`；第 2 回合 `斩烈火`；第 3 回合 `护斩`；第 4 回合 `奖励1、斩`。  
- **山鬼**：开局 `镇斩护、奖励1`；第 2 回合 `破斩`；第 3 回合 `奖励2、聚`；第 4 回合 `烈火护`。  
- 奖励插入：`Stage_*.rewardInsertions`。恢复菜单：`AR封妖/按策划重置符匣与火符`。

### 3.4 卡牌 id ↔ 中文名

| 文件名 | cardName | 类型 |
|--------|----------|------|
| `attack` | 斩妖符 | Attack / 红 |
| `defense` | 护身符 | Defense |
| `break` | 破煞符 | ArmorBreak / 黄 |
| `hp` | 聚气诀 | Ability |
| `seal` | 镇魂符 | Seal / 紫 |
| `fire` | 烈火符 | Fire 灼烧 1 |
| `reward_lianzhan` | 连斩符 | 奖励 Attack |
| `reward_lianhuo` | 炼火符 | 奖励 Fire 灼烧 2 |
| `reward_zhenhunling` | 镇魂铃 | 奖励 Seal |
| `reward_pozhen` | 破阵斩 | 奖励 Attack |
| `reward_yinhuo` | 引火诀 | 奖励 引爆 |
| `reward_dinghun` | 定魂符 | 奖励 Seal |
| `ring` | 测试牌_勿用 | 废牌 |

### 3.5 出牌方式

- **箭头指向**：Attack / ArmorBreak / Seal / Fire  
- **上拖打出**：Defense / Ability  

---

## 4. 运行时架构（简）

```
BattleBootstrap → BattleFlowManager → 换模 ActiveMonster + WeaknessAnchorSetup → TurnManager
出牌 CardDragHandler → 弱点射线 → CardManager → 匹配则 QTE → 成功后 UI 关再结算
结束回合 → 敌人意图结算 → 灼烧 → 下回合
通关 → RewardSelectUI.Show（实例化真实 Card 预制体）→ 选中 → 下一关
```

---

## 5. 关键脚本

```
Assets/Scripts/
  Campaign/BattleFlowManager.cs
  Character/Mono/
    WeaknessPoint.cs / WeaknessAnchorSetup.cs
    EnemyIntentController.cs / CharacterStats.cs
  Card/Mono/
    Card.cs              # Init：图案+费用+类型+描述
    CardDragHandler.cs / CardDeck.cs
  Managers/
    CardManager.cs       # 对象池 GetCardObj → Card.prefab
    CardCameraManager.cs # Card 层 Overlay 相机
    TurnManager.cs / BattleBootstrap.cs
  UI/
    BattleHudAdjustableController.cs  # 血/灵气/护甲/弱点图标/灼烧
    BattleHudInfoPresenter.cs         # 名、意图(一行)、回合状态、提示
    RewardSelectUI.cs                 # ⭐ 奖励：实例化 Card.prefab
    QTEPanelUI.cs / DamagePopup.cs / PlayerDamageFeedback.cs …
  Utilities/TmpChineseFontUtil.cs     # 绑中文字体 2/3 + 补字 + 材质同步

Assets/Editor/
  ApplyPlanCardData.cs / SetupRewardCardPresentation.cs
  BuildAdjustableBattleHudSkin.cs / SetupMonsterPrefabWeaknesses.cs
  RebuildChineseFonts.cs
```

---

## 6. 编辑器菜单（AR封妖）

| 菜单 | 作用 |
|------|------|
| `按策划重置符匣与火符` | 符匣顺序 + 卡图命名 + 奖励插入 |
| `配置奖励三选一卡牌界面` | 绑定 `Card.prefab` 到 `RewardSelectUI` |
| `配置怪物Prefab弱点（头部）` | 三关 Prefab 头骨挂弱点 |
| `重建镇魂铃QTE界面` | 只重建 QTE |
| `重建中文字体图集` | TMP 2/3/ziti 烘焙汉字 |
| `搭建可手动调整的战斗HUD` | **慎用**，会覆盖手调布局 |
| `重建战斗HUD（横屏）` | **旧方案，勿随意用** |

---

## 7. 已知问题 / 注意点

### 7.1 弱点（已修复并优化）

- 弱点点位（红/黄/紫）已通过 `WeaknessAnchorSetup.ApplyForStage` 永久锚定到对应怪物的头部骨骼（`Vespomorph_Head` / `CAVECRAWLER_HEAD` / `Drackmahre_ Head`），且已将场景存盘。
- 弱点隐藏了默认的不良粒子特效以避免红色 billboard 方块，保留了纯净的材质球呼吸灯与世界碰撞体。
- 运行时已整合“未识别扫卡不显示弱点/怪物模型”的显隐保护，由 `BattleFlowManager.SetMonsterModelVisibility` 控制。

### 7.2 UI / 字体 / 渲染 / 避坑指南

1. **TMP 中文**：必须 `TmpChineseFontUtil.Apply` / `BindChineseFont`（换字体时同步 material）。优先字体资产 `2`。  
2. **Boss 血条节点 `BossHealth_可调` 常有非均匀 scale（如 Y≈0.19）**。名称/意图**不要挂在其下**，否则字被压扁；`BattleHudInfoPresenter` 会把它们 detach 到 `HUD_ArtSkin_Adjustable` 根。  
3. **意图文案只要一行名**（如「普通攻击」），不要叠伤害/破绽说明。  
4. **意图底框**随文字 preferred 宽高收紧（`FitIntentBoxToText`）。  
5. **奖励卡**：必须用 `Card.prefab`，禁止再 UI 重拼底+图；见 §16。  
6. **奖励遮罩**：展示时 Canvas 临时 `Screen Space - Camera`，CardCamera 在栈末，卡在遮罩前；关掉恢复 Overlay。  
7. QTE 成功：面板关闭后再跳字/扣血。  
8. 灼烧独立行，禁止把 `<color>` 拼进意图字符串。  
9. 三怪物 Prefab 序列化在 `BattleFlowManager`，禁止改回仅 Editor 加载。
10. **TurnManager 预设激活状态坑**：`TurnManager` 组件的 `isBattleActive` 在场景或预制体中必须默认为 `false`！否则扫卡时会被误判为“丢失识别重连”而拦截开战。我们在 `TurnManager.Awake` 中加入了强制初始化 `isBattleActive = false` 的兜底。

---

## 8. 下一步（按优先级）

### P0

- [ ] **真机全流程物理扫图测试**（真机打通三关、扫实体卡牌对战、奖励选取、解锁新关）
- [ ] **UI 安全区适配**（确保结束回合与边缘文本在全面屏或不同手机设备上不被遮挡）

### P1

- [ ] **受击震动与 QTE 音效**（强化交互反馈打击感）
- [ ] **怪物攻击/受击/死亡动画对齐**（确保数值结算跳字与动画动作节奏咬合）

### P2

- [ ] **精准圆环 QTE / Boss 多阶段等精修特性**

---

## 9. 建议 of 下一条任务

**首选：** 真机物理扫图全流程验证（确保 Vuforia 数据流在 Android/iOS 运行流畅无卡点）。

**次选：** 适配 UI 安全区边界。

**再次选：** 音效与受击震动反馈接入。

---

## 10. 快速自测清单

1. 开局手牌 **斩、斩、护、聚**。  
2. 新 HUD：血/灵气/护甲/灼烧/结束回合正常。  
3. 意图仅显示如「普通攻击」一行，清晰不压扁。  
4. 右侧：第 N 回合 + 玩家/妖怪 + 牌堆/手牌，中文无方框。  
5. 拖弱点 → QTE → 成功后 UI 消失再跳字。  
6. 奖励：三张与手牌**同款**完整卡，在暗色遮罩**前方**可点，居中。  
7. 三关换模；失败重试；灼烧逻辑。  

---

## 11. 技术备忘

- Unity 6：`FindObjectsByType` / `FindAnyObjectByType`。  
- 手牌：`Card` 层 + `CardCamera` Overlay；`CardCameraManager`。  
- TMP：动态图集 + Multi Atlas；`RebuildChineseFonts` 可烘焙。  
- Grok + Unity MCP：`localhost:8080/mcp`（断连需重连）。  

---

## 12. Git 备忘

- 曾用分支 `codex/pre-boss-merge-20260711` 合并 Boss 模型。  
- 易冲突：`SampleScene.unity`、`RewardSelectUI.cs`、`BattleHudInfoPresenter.cs`、`PROJECT_SUMMARY.md`。  

---

## 13. 本轮（2026-07-12）完成摘要

| 项 | 结果 | 说明 |
|----|------|------|
| 新版战斗 HUD | ✅ 已验收 | 用户验收整体 OK |
| 意图文案 | ✅ 已完成 | 仅一行意图名；中文字体/缩放修复 |
| 意图底框 | ✅ 已完成 | 随文字收紧 |
| 回合状态 | ✅ 已完成 | 回合数+阶段+牌堆/手牌 |
| BossHealth 非均匀 scale | ✅ 已完成 | 名/意图脱离该父节点 |
| 奖励 UI | ✅ 已完成 | **实例化 Card.prefab** + Init 数据 |
| 奖励被遮罩挡住 | ✅ 已完成 | 临时 Screen Space Camera + 透明 Panel + CardCamera 置顶 |
| **三关战役完全解耦** | ✅ 已完成 | 三张 AR 卡分别独立触发三关，支持线性关卡校验，未扫到卡不显示模型且不开战 |
| **三色弱点绑定** | ✅ 已完成 | 三关怪物的弱点已在编辑器下永久精确挂接到各自头部骨骼，且已对齐其生命周期 |
| **模拟扫卡模型浮现** | ✅ 已完成 | 修复了 Vuforia 在未识别时强行关闭子物体 Renderer 等造成的模型不可见问题 |
| **手牌隐藏 Bug 修复** | ✅ 已完成 | 恢复了 `BattleBootstrap` 规范的开战时序；并在开战时动态强刷 URP 渲染堆栈防 Vuforia 覆盖；解决了 `TurnManager` 初始被误标为 Active 导致的模拟阻断 |

---

## 14. 现有 UI 地图（Canvas）

| 节点 | 职责 |
|------|------|
| `HUD_ArtSkin_Adjustable` | **当前战斗 HUD 源头**（可手调） |
| 旧 `HUD_Enemy/Player/SideInfo/Actions` | **保持隐藏** |
| `HUD_QTE` | 镇魂铃 QTE |
| `HUD_Reward` | 标题+压暗；卡是世界空间 Card 实例 |
| `HUD_Result` | 胜负 |
| `PF_StartIntro` | 开场 |

---

## 15. 新版战斗 HUD 维护（重要）

> **优先改现有可调节点。不要随便跑「重建战斗HUD」或挂回 `BattleHudArtSkin`。**

### 15.1 可手调节点（`Canvas/HUD_ArtSkin_Adjustable`）

| 节点 | 用途 |
|------|------|
| `BossHealth_可调` | Boss 血条（**常有非均匀 scale**） |
| `BossName_可调` | Boss 名（应在 HUD 根下，scale=1） |
| `EnemyIntent_可调` | 意图**一行字**（HUD 根下，scale=1） |
| `WeaknessIcon_*` / `Burn*` | 弱点色/灼烧 |
| `PlayerHealth/Energy/Armor_可调` | 玩家状态 |
| `EndTurn_Adjustable` | 结束回合 |
| `TurnInfoPanel_可调` → `TurnState_可调` / `Hint_可调` | 回合与提示 |

### 15.2 脚本

- **`BattleHudAdjustableController`**：数值与图标裁切。  
- **`BattleHudInfoPresenter`**：  
  - 意图：`CurrentStep.displayName` **仅一行**（普通攻击/正在防御/蓄力中…）  
  - 回合：第 N 回合、玩家/妖怪、牌堆剩余与手牌  
  - 提示：弱点对应符等  
  - 中文字体强制 `2` + material；意图底框 `FitIntentBoxToText`  
  - `detachTextFromScaledParents`：离开 `BossHealth` 非均匀 scale  

### 15.3 字体

- 新建中文 TMP 必调 `TmpChineseFontUtil.Apply`。  
- 缺字：`AR封妖/重建中文字体图集`。  

---

## 16. 卡牌结构与奖励 UI（必读）

### 16.1 数据与卡面分离

| 部分 | 位置 | 说明 |
|------|------|------|
| **数据** | `Assets/Game Data/Card Data/*.asset` | `CardDataSO`：名/费用/类型/描述/`cardImage`（**仅中间图案**） |
| **卡面组装** | `Assets/Prefabs/Card/Card.prefab` | 已拼好的完整卡 |

**Card.prefab 大致层级（Entry 下）：**

```
card_background  ← 卡牌底.png
card_type        ← 顶部文字注释.png + 类型 TMP
card_slot        ← 卡牌左上角费用.png + 费用 TMP
圆框             ← 圆框.png
  └ card_sprite  ← CardDataSO.cardImage（符咒图案）
描述 TMP
```

`Card.Init(data)` 写入：`cardSprite`、费用、类型、描述。

### 16.2 奖励三选一（当前实现）

- 脚本：`RewardSelectUI.cs`  
- **做法**：`Instantiate(Card.prefab)` → `Card.Init(rewardSO)` → 放 Card 层 → `CardCamera` 渲染  
- **不要**再用 UI Image 重拼底+图（细节会对不齐）。  
- 关掉 `CardDragHandler`；`RewardCardPickProxy` 点选。  
- 展示时：藏手牌；Canvas 临时 **Screen Space - Camera**；Panel 底透明；CardCamera 栈顶。  
- 关闭时：销毁奖励卡实例；恢复手牌与 Canvas Overlay。  
- 配置菜单：`AR封妖/配置奖励三选一卡牌界面`（绑定 `cardPrefab`）。  
- 调大小/间距：`displayScale` / `worldSpacing` / `viewLocalCenter`（Inspector）。  

---

**交接结论**：玩法可演示；**新版 HUD 与奖励（真 Card 预制体）已落地**。  
**新聊天优先**：① 弱点精修 ② 真机回归 ③ 音效/动画。  
开场请读 **§0 / §2 / §8 / §15 / §16**。
