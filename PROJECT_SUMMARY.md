# AR封妖牌局 — PROJECT_SUMMARY（交接用）

> **用途**：新对话 / 新同学 / 新 agent 接入时先读本文件。  
> **更新日期**：2026-07-12（可手调战斗 HUD、中文 TMP 修复、完整卡牌奖励三选一已落地；弱点仍有 bug 暂挂）  
> **策划案**：根目录 `AR封妖牌局_第一版玩法策划案.docx`  
> **UI 参考**：根目录 `UI设计参考图.png`（横屏简易 HUD）  
> **铃铛图**：`Assets/_ARSealCardGame/Art/UI/QTE/bell.png`（由根目录 `铃铛.png` 导入）  
> **卡面美术**：`Assets/Prefabs/Cardpng/`（各符咒成品图 + 外框/费用等零件）

---

## 0. 新对话怎么开（复制即可）

### 0.1 做新版 UI（当前优先）

```text
请先读 F:\AR-Card-Game\PROJECT_SUMMARY.md，重点看 §2 / §5 / §6 / §14（现有 UI 地图）与 §8（下一步）。
工程是 Unity 6 URP + Vuforia AR 卡牌。主场景 Assets/Scenes/SampleScene.unity。
当前任务：搭建新版战斗/奖励/QTE 等 UI，可参考 UI设计参考图.png 与 Cardpng 卡面资源。
玩法与数据已基本闭环，不要破坏 BattleFlowManager / 符匣顺序 / 卡牌 SO 逻辑。
弱点仍有已知 bug，本轮可先不动弱点，除非 UI 依赖其显示。
```

### 0.2 其它任务示例

- 「继续修弱点：别陷进模型、更明显」
- 「真机全流程回归」
- 「玩家受击加手机震动」

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
| 符匣固定顺序 | ✅ | 已按策划案 V1.0 重置（见 §3.3）；菜单可一键恢复 |
| 卡牌命名/贴图 | ✅ | 资源英文 id + 中文 `cardName`；`Cardpng` 已绑定；运行时实例名 `Card_斩妖符` |
| 三色弱点 + QTE | 🔧 | 逻辑可用；弱点挂怪物 Prefab 头骨、仅小光球；**仍有显示/位置 bug，待修** |
| 镇魂铃 QTE UI | ✅ | 全屏压暗 + 居中大铃铛；2 秒点 3 次；成功后 UI 关闭再结算 |
| 伤害跳字 | ✅ | `DamagePopup` 世界空间数字；QTE 成功金色更大 |
| 怪物弱点色闪 | ✅ | `MonsterHitFlash` |
| 玩家受击反馈 | ✅ | `PlayerDamageFeedback`：HUD 抖 + 全屏红闪 |
| 敌人意图 | ✅ | 攻击→防御→蓄力；只亮对应弱点 |
| 横屏 HUD | ✅ 可换皮 | 顶敌人（名/血/意图/**独立灼烧行**）、右上玩家、右中提示、右下结束回合 |
| 三关战役 + Boss 模型 | ✅ | 运行时 `ActiveMonster` 换皮；编辑器 Ellen 占位正常 |
| 奖励三选一 | ✅→🔧 | **已显示卡面图+名称+描述**（`RewardSelectUI`）；可再做成完整卡 UI |
| 灼烧构筑 | ✅ | 烈火 1 层 / 炼火 2 层；敌方行动后每层 1 伤；引火诀引爆 |
| 开始界面 | ✅ | 唯一 `PF_StartIntro`；动画完再开战 |
| 手机 AR | ✅ | 用户已验证扫图；Editor `skipARForEditor` |

**相对策划案仍缺**：新版正式 UI、精准 QTE、Boss 多阶段、更丰富封印演出、弱点精修、受击音效/手机震动、真机「未识别不开战」门闩（可选）。

---

## 3. 战役与数据一览

### 3.1 流程

```
开始界面(PF_StartIntro，播放完毕)
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
| 敌人数值 | `enemy_xiaoyao` / `enemy_shiling` / `enemy_shangui` |
| 符匣 | `Assets/Game Data/Card Library/FuXia_XiaoYao\|ShiLing\|ShanGui.asset` |
| 基础牌 SO | `Assets/Game Data/Card Data/attack, defense, break, fire, seal, hp` |
| 奖励牌 SO | `reward_lianzhan, reward_lianhuo, reward_zhenhunling, reward_pozhen, reward_yinhuo, reward_dinghun` |
| 玩家 | `player.asset`（50HP / 3 灵气） |
| 怪物预制体 | `Assets/fbx/monsters/1/Prefabs/Vespomorph`、`2/Cavecrawler`、`3/Drackmahre` |
| 手牌 Prefab | `Assets/Prefabs/Card/Card.prefab`（世界空间，非 UI Canvas） |
| 卡面图 | `Assets/Prefabs/Cardpng/*.png`（完整符咒图 + 框/底/费用零件） |
| QTE 美术 | `Assets/_ARSealCardGame/Art/UI/QTE/bell.png`、`ripple_ring.png` |
| 中文字体 | `Assets/Scripts/Utilities/2.asset`、`3.asset`、`ziti.asset`（优先 2/3；ziti fallback→2） |

### 3.3 固定补牌顺序（策划案 V1.0，已写入符匣）

- **小妖**：开局 `斩、斩、护、聚`；第 2 回合 `斩、烈火`；第 3 回合 `护、斩`（尾部填充破/镇）。  
- **石灵**：开局 `破、斩、护、聚`；第 2 回合 `斩、烈火`；第 3 回合 `护、斩`；第 4 回合 `奖励 1、斩`。  
- **山鬼**：开局 `镇、斩、护、奖励 1`；第 2 回合 `破、斩`；第 3 回合 `奖励 2、聚`；第 4 回合 `烈火、护`。  
- 奖励插入时机在各 `Stage_*.asset` 的 `rewardInsertions`。  
- 一键恢复：`AR封妖/按策划重置符匣与火符`。

### 3.4 卡牌 id ↔ 中文名（调试用）

| 文件名 | cardName | 类型 |
|--------|----------|------|
| `attack` | 斩妖符 | Attack / 红弱点 |
| `defense` | 护身符 | Defense |
| `break` | 破煞符 | ArmorBreak / 黄弱点 |
| `hp` | 聚气诀 | Ability |
| `seal` | 镇魂符 | Seal / 紫弱点 |
| `fire` | 烈火符 | Fire / 灼烧 1 |
| `reward_lianzhan` | 连斩符 | 奖励 Attack |
| `reward_lianhuo` | 炼火符 | 奖励 Fire / 灼烧 2 |
| `reward_zhenhunling` | 镇魂铃 | 奖励 Seal |
| `reward_pozhen` | 破阵斩 | 奖励 Attack |
| `reward_yinhuo` | 引火诀 | 奖励 Fire / 引爆 |
| `reward_dinghun` | 定魂符 | 奖励 Seal |
| `ring` | 测试牌_勿用 | 废牌，勿进正式流程 |

运行时手牌 GameObject 名：`Card_{cardName}`（如 `Card_斩妖符`）。

### 3.5 卡牌类型与出牌方式

`Attack` | `Defense` | `Ability` | `ArmorBreak` | `Seal` | `Fire`  
弱点：`RedAttack` / `YellowArmor` / `PurpleSeal`

- **箭头指向**：Attack / ArmorBreak / Seal / Fire  
- **上拖打出**：Defense / Ability  

---

## 4. 运行时架构（谁调用谁）

```
BattleBootstrap.BeginBattle()
  └─ BattleFlowManager.StartCampaign()
       └─ ApplyStageToBattle
            ├─ SwapMonsterModel → ActiveMonster 实例化（怪物 Prefab，含弱点）
            ├─ WeaknessAnchorSetup.ApplyForStage  // 关场景旧弱点；头骨外推
            └─ TurnManager.StartBattle()
                 └─ PlayerDamageFeedback.EnsureExists()

玩家出牌 CardDragHandler
  ├─ 拖动中 UpdateWeaknessAim → WeaknessPoint.SetAimed 高亮
  └─ RaycastAll 优先 WeaknessPoint
       └─ CardManager.ExecuteCard
            └─ 匹配弱点 → QTEManager.StartClickQTE
                 ├─ 成功：等 HUD_QTE 关闭 → 伤害/跳字/MonsterHitFlash
                 └─ 失败：立刻结算普通伤害

敌人攻击 player.TakeDamage
  └─ OnTookHit → PlayerDamageFeedback（HUD 抖 + 红闪）

结束回合 → EnemyActionFlow → 灼烧结算 → 下回合 PresentIntent

通关 → RewardSelectUI.Show（卡面图+文案）→ 选中后进下一关
```

---

## 5. 关键脚本地图

```
Assets/Scripts/
  Campaign/
    BattleFlowManager.cs      # 三关 + 换模 + WeaknessAnchorSetup + rewardUI
    BattleStageSO.cs / CampaignSO.cs
  Character/Mono/
    WeaknessPoint.cs          # 仅小光球 + SphereCollider；无粒子；瞄准高亮
    WeaknessAnchorSetup.cs    # 关 Ellen 旧弱点；挂 ActiveMonster 头骨并外推
    CharacterStats.cs         # OnTookHit
    EnemyIntentController.cs  # 优先扫 ActiveMonster 上弱点
    MonsterAnimationBridge.cs
  Combat/
    QTEManager.cs
  Card/Mono/
    Card.cs                   # Init 设名 Card_{中文}；刷 cardImage
    CardDragHandler.cs
    CardDeck.cs               # 固定顺序抽牌 + 奖励插入
    CardLayoutManager.cs
  Card/SO/
    CardDataSO.cs / FuXiaOrderSO.cs / CardLibrarySO.cs
  UI/
    QTEPanelUI.cs             # 镇魂铃 QTE
    DamagePopup.cs
    MonsterHitFlash.cs
    PlayerDamageFeedback.cs
    BattleInfoUI.cs           # 敌人顶栏 / 灼烧独立行 / 玩家状态
    RewardSelectUI.cs         # ⭐ 奖励三选一：卡面 Image + 名 + 描述
    BattleResultUI.cs
  Utilities/
    TmpChineseFontUtil.cs     # 动态补字；优先字体 2/3
  Managers/
    TurnManager.cs / CardManager.cs / BattleBootstrap.cs

Assets/Editor/
  ApplyPlanCardData.cs        # 按策划重置符匣/贴图/命名；重建奖励 UI 布局
  SetupMonsterPrefabWeaknesses.cs
  SetupWeaknessAndQTE.cs / SetupMultiWeakness.cs / SetupCampaign.cs
  BuildBattleHud.cs           # 重建整套横屏 HUD（慎用）
  RebuildChineseFonts.cs
```

---

## 6. Unity 编辑器菜单（AR封妖）

| 菜单 | 作用 |
|------|------|
| `AR封妖/重建战斗HUD（横屏）` | 整套 HUD（**慎用**，会清子物体） |
| `AR封妖/修复敌人状态UI布局` | 只加高敌人顶栏 + 独立灼烧行 |
| `AR封妖/重建镇魂铃QTE界面` | **只重建 QTE**，不碰弱点 |
| `AR封妖/按策划重置符匣与火符` | 符匣顺序 + 卡图命名 + 奖励插入 + 奖励面板尺寸 |
| `AR封妖/命名卡牌并绑定贴图` | 仅卡牌 SO 名/图 |
| `AR封妖/重建奖励三选一卡面UI` | 重建 `HUD_Reward` 布局 |
| `AR封妖/配置怪物Prefab弱点（头部）` | 三关 Prefab 头骨挂弱点 |
| `AR封妖/配置三关战役` | 战役 SO + BattleFlowManager |
| `AR封妖/重建中文字体图集（修复乱码）` | TMP 2/3/ziti 烘焙汉字 |

**场景应有**：`ARCamera`、`ImageTarget`、`Ellen_skin (2)`（逻辑宿主 + Intent + CharacterStats）、`BattleFlowManager`（三预制体引用）、`Canvas`（HUD_* / HUD_QTE / HUD_Reward）、`QTEManager`、`CardDeck`。

**编辑器 vs 真机模型**：场景里永久是 Ellen 占位；Play/真机开战由 `SwapMonsterModel` 挂 `ActiveMonster`。属设计如此。

---

## 7. 已知问题 / 注意点

### 7.1 弱点（已知仍有 bug — 新 UI 可先不动）

1. **已做**：无粒子（避免红方块）；仅 Unlit 小光球；Collider 写在怪物 Prefab 头骨下；运行时外推；场景 Ellen 旧弱点禁用。  
2. **头骨名**：  
   - 小妖 `Vespomorph_Head`  
   - 石灵 `CAVECRAWLER_HEAD`  
   - 山鬼 `Drackmahre_ Head`（**下划线后有空格**）  
3. **用户反馈仍有问题**（位置/嵌模/不够显眼等）——后续单独修。  
4. **调参**：Prefab 上 `Weakness_Red/Yellow/Purple` 的 `localPosition` / `hitRadius` / `visualCoreScale`；或改 `WeaknessAnchorSetup.PlaceOutsideHead`。  
5. 射线：`CardDragHandler.ResolveAttackTarget` 必须 `RaycastAll` 优先弱点。

### 7.2 其它注意（做 UI 必看）

1. **QTE 成功结算时机**：结果 UI 关闭后才跳字/扣血/闪色。  
2. **QTE 首次显示**：`QTEPanelUI.Awake` 禁止 `HideImmediate`。  
3. **敌人灼烧**：必须独立 `enemyBurnText`，禁止把 `<color>` 拼进意图文本。  
4. **中文乱码**：TMP 缺字 → 跑「重建中文字体图集」；优先字体 `2`/`3`。  
5. **红闪 Image 必须有 Sprite**，否则电脑上看不见。  
6. **手牌是世界空间**（CardCamera + `Card` 层），不是 Canvas UI；新版 UI 若改手牌展示需考虑双相机。  
7. **奖励 UI**：`RewardSelectUI` 运行时生成卡面按钮；`cardImage` 来自 SO；无图则灰底。  
8. **模型**：三预制体序列化在 `BattleFlowManager`，禁止改回仅 Editor 加载。  
9. **开始界面**：仅一个 `PF_StartIntro`。  
10. **灼烧**：敌方行动后结算；引火诀引爆清层。

---

## 8. 下一步必须做什么（按优先级）

### P0 — 新版 UI 搭建（当前主线，给 UI agent）

- [ ] 对照 `UI设计参考图.png` 规划横屏布局（安全区、AR 中间留白）  
- [ ] 战斗 HUD：敌人顶栏（名/血/意图/灼烧）、玩家（血/灵气/护甲）、结束回合、提示条  
- [ ] 奖励三选一：用 `Cardpng` 做成更接近正式卡面的选项（可替换/增强 `RewardSelectUI`）  
- [ ] QTE 面板：保持玩法（2 秒 3 点）前提下换皮  
- [ ] 开始界面 / 胜负结算 UI 与新风格统一  
- [ ] 中文 TMP：继续用字体 `2`/`3` + `TmpChineseFontUtil`  
- [ ] **不要破坏**：`BattleFlowManager`、符匣顺序、出牌逻辑、弱点射线、关卡 SO 数据

### P0 — 弱点（可并行，但非本轮 UI 阻塞）

- [ ] 修嵌模/不明显；真机与 Editor 都可点  
- [ ] 确认瞄准高亮 + QTE 触发稳定  

### P0 — 发布向

- [ ] 真机全流程：扫图 → 三关 → 两次奖励 → 全通 / 失败重试  

### P1 — 表现

- [ ] 受击震动 / QTE 音效 / 动画对齐战斗事件  

### P2 — 策划剩余

- [ ] 精准圆环 QTE、连斩条件、Boss 多阶段  

---

## 9. 建议的下一条任务（直接开干用）

**首选（当前）：**

> 搭建新版战斗 UI：读 `UI设计参考图.png` 与现有 `Canvas` 下 `HUD_*`，在不拆玩法逻辑的前提下换皮/重做横屏 HUD、奖励卡面、QTE 面板；卡面资源用 `Assets/Prefabs/Cardpng/`。

**次选：**

> 修弱点嵌模与可见度（Prefab 头骨偏移 / `WeaknessAnchorSetup.PlaceOutsideHead`）。

**再次选：**

> 真机全流程回归 + 问题清单。

---

## 10. 快速自测清单（改完必过）

1. 开始界面一次点击 → 动画完进第 1 关；开局 **斩、斩、护、聚**。  
2. 小妖模型为 Vespomorph；弱点可见（允许仍有位置 bug）。  
3. 拖斩妖符对准弱点 → QTE 铃铛 → 成功后 UI 消失再跳字/闪色。  
4. 结束回合若敌人攻击：HUD 抖 + 红闪。  
5. 叠灼烧后顶栏独立「灼烧 n 层」，无 `<color=` 字样。  
6. 奖励三选一：**有卡面图** + 中文可读。  
7. 三关切换模型；失败重试；全通。  
8. 烈火/炼火/引火灼烧逻辑正确。  
9. Hierarchy 手牌名为 `Card_斩妖符` 等，便于调试。

---

## 11. 技术备忘

* Unity 6：`FindObjectsByType` / `FindAnyObjectByType`。  
* Grok + Unity MCP：`http://localhost:8080/mcp`；Unity 开 MCP Bridge（断连时需重连）。  
* DOTween：UI `DOShakeAnchorPos`、`Image.DOColor`（Image 需 Sprite）。  
* TMP：`atlasPopulationMode=Dynamic` + `isMultiAtlasTexturesEnabled`；毛笔字体靠 fallback。  
* 新 UI 建议：Screen Space Overlay/Camera 的 Canvas 与现有世界空间手牌分离，避免改坏 CardCamera。

---

## 12. Git / 分支说明

### 12.1 历史

- 分支 `codex/pre-boss-merge-20260711` 曾合并 Boss 模型相关改动。  
- 合并提交参考：`8b2e89f`；安全点：`0ae1457`。  

### 12.2 当前

- **已与同伴合并完成**；卡牌资源已装上。  
- 本轮（合并后）改动：弱点 Prefab 化、去粒子、符匣重置、卡图绑定、奖励卡面 UI、`PROJECT_SUMMARY` 更新。  
- 易冲突文件（UI 重做时注意）：  
  - `SampleScene.unity`（Canvas / HUD）  
  - `BuildBattleHud.cs` / `RewardSelectUI.cs` / `QTEPanelUI.cs` / `BattleInfoUI.cs`  
  - `PROJECT_SUMMARY.md`  

---

## 13. 本轮（合并后）已完成摘要

| 项 | 结果 |
|----|------|
| 同伴合并 | ✅ 完成，卡牌资源已在 |
| 弱点去红方块 | ✅ 去掉粒子，仅小光球 |
| 弱点挂 Prefab 头骨 | ✅ 三关；山鬼头骨名带空格 |
| 弱点仍有 bug | 🔧 用户确认还要再修，**先让路给新 UI** |
| 符匣策划顺序 | ✅ 三关已重置 |
| 卡牌命名+贴图 | ✅ 英文 id + 中文名 + Cardpng |
| 奖励三选一卡面 | ✅ Image+名+描述（可再美化） |
| 手牌调试名 | ✅ `Card_{中文名}` |

---

## 14. 现有 UI 地图（新版 UI agent 必读）

场景 `Canvas` 下（名称可能随重建变化，以场景为准）：

| 节点 / 脚本 | 职责 | 新版 UI 建议 |
|-------------|------|----------------|
| `HUD_*` + `BattleInfoUI` | 敌人顶栏、玩家状态、提示、结束回合 | 换皮时**保留字段绑定**（血条/意图/灼烧/灵气） |
| `HUD_QTE` + `QTEPanelUI` | 镇魂铃点击 QTE | 可换美术；勿改「2 秒 3 点 + 成功后再结算」时序 |
| `HUD_Reward` + `RewardSelectUI` | 奖励三选一 | 已有卡面槽；可改成完整卡 Prefab |
| `HUD_DamageFlash` + `PlayerDamageFeedback` | 受击红闪+抖 | 保留触发入口 `CharacterStats.OnTookHit` |
| 手牌 `Card` Prefab | 世界空间手牌 | 与 Canvas 分离；改 UI 时别误删 CardCamera |
| `PF_StartIntro` | 开场动画 | 播完再 `BeginBattle` |
| `BattleResultUI` | 胜负/重试 | 与新风格统一 |

**数据流（UI 只读/回调，勿把逻辑写死在按钮里）：**

- 状态显示 ← `CharacterStats` / `EnemyIntentController` / `BattleInfoUI`  
- 结束回合 → `TurnManager`  
- 奖励选择 → `BattleFlowManager` 回调  
- QTE → `QTEManager` ↔ `QTEPanelUI`  

---

**交接结论**：玩法闭环可演示；数据与符匣已按策划案归位。  
**新 agent 优先**：**新版 UI 搭建**（§8 P0 / §14）。弱点 bug 与真机回归可并行，但不阻塞 UI。  
开场请读 **§0.1 / §2 / §8 / §14**。

---

## 15. 2026-07-12 可维护新版战斗 UI（当前 UI 源头）

> **重要：用户已在 Unity 中精细调整过布局。后续维护优先改现有节点，不要运行旧的「重建战斗HUD（横屏）」菜单，也不要重新挂载旧版 `BattleHudArtSkin`。**

### 15.1 场景层级与可手调节点

场景：`Assets/Scenes/SampleScene.unity`  
根节点：`Canvas/HUD_ArtSkin_Adjustable`

该根节点和所有子物体已保存到场景中，停止 Play 后仍可在 Hierarchy/Inspector 直接调整：

| 节点 | 用途 | 手调说明 |
|---|---|---|
| `BossHealth_可调` | Boss 分层血条 | 调整体位置/尺寸；不要手调其 `FillMask_生命裁切` 宽度 |
| `BossName_可调` | Boss 名称 | 血条上方，跟随 `BossHealth_可调` |
| `EnemyIntent_可调` | 怪物攻击意图 | 血条下方，跟随 `BossHealth_可调` |
| `WeaknessIcon_Adjustable` / `BurnIcon_Adjustable` / `BurnCount_可调` | 弱点、灼烧图标与层数 | 直接调 RectTransform |
| `PlayerHealth_可调` / `PlayerEnergy_可调` / `PlayerArmor_可调` | 玩家血量、灵气、护甲 | 直接调 RectTransform；玩家生命从下向上裁切 |
| `EndTurn_Adjustable` / `Label_回合结束` | 回合结束按钮及文字 | 两者会跟随；按钮保留 `TurnManager.EndPlayerTurn` 回调 |
| `TurnInfoPanel_可调` | 右侧回合状态与操作提示 | 直接移动整个面板或内部 `TurnState_可调`、`Hint_可调` |

### 15.2 运行时脚本职责（不要混用）

- `BattleHudAdjustableController.cs`：只更新 Boss/玩家血量、灵气、护甲、弱点、灼烧；根节点 Inspector 的 `Boss Full Width` 与 `Player Fill Full Height` 必须分别和用户调后的血条填充范围一致。
- `BattleHudInfoPresenter.cs`：更新 Boss 名称、**完整攻击意图**（类型/伤害或护甲/破绽与推荐符）、**回合状态**（第 N 回合、玩家/妖怪阶段、符匣剩余与手牌）、情境操作提示、「回合结束」文字；按破绽上色并可选意图半透明底；不重建用户手调布局。
- `BattleHudArtSkin.cs`：**旧的运行时生成方案，当前场景不应再挂载**；挂回去会覆盖/重复新 HUD。
- `TmpChineseFontUtil.cs`：`Apply` 会优先使用项目内 `2` / `3` 中文 TMP 字体并补齐动态字符；新建任何中文 TMP 文本后必须调用 `TmpChineseFontUtil.Apply(text, value)`，否则可能显示方块。

### 15.3 旧 HUD 与美术资源

- 旧 `HUD_Enemy`、`HUD_Player`、`HUD_SideInfo`、`HUD_Actions` 已整体隐藏，用于清除右侧残留文字；不要重新启用它们，避免和 `HUD_ArtSkin_Adjustable` 重叠。
- `HUD_QTE`、`HUD_Reward`、`HUD_Result` 没有被这次 HUD 换皮删除；QTE 仍保持「2 秒内点 3 次，成功结果 UI 关闭后结算」逻辑。
- 新 HUD 使用的分层 PNG 位于 `Assets/Resources/BattleHudSkin/`；原始文件仍在根目录 `UI美术资产/`。`RawImage.uvRect` 是为大画布美术做的裁切，若图层边缘不对，优先在 Inspector 调对应 `RawImage` 的 `UV Rect`，不要替换业务脚本。

### 15.4 奖励三选一卡牌 UI

- `RewardSelectUI.cs` 已改为运行时生成 `RewardChoiceCard_0~2`：每张选择项均由 `卡牌底.png`、裁切后的符咒图案、名称/费用、类型/说明组成，整张卡可点击。
- 生成位置：`Canvas/HUD_Reward/Panel/Choices/RewardChoiceCard_*`（仅 Play 或显示奖励时出现）。
- 正式底图引用字段：`RewardSelectUI.cardBaseSprite`，已绑定 `Assets/Prefabs/Cardpng/卡牌底.png`。
- 若场景引用丢失，可运行菜单：`AR封妖/配置奖励三选一卡牌界面`。不要回退到旧的纯文字/图标按钮。

### 15.5 维护菜单与回归

- `AR封妖/搭建可手动调整的战斗HUD`：只在新场景丢失整个 `HUD_ArtSkin_Adjustable` 时使用；会重建基础节点，**会覆盖用户手调布局**。
- `AR封妖/补充可调HUD文字框`：仅在文字节点整体丢失时使用；会重建 Boss 名称、意图、右侧提示与结束回合文字。
- 每次改 UI 后至少验证：开战时 Boss/玩家数值更新、结束回合按钮可用、攻击意图随回合切换、中文无方块、奖励弹出时存在三张完整可点击卡牌、QTE 能正常完成。
