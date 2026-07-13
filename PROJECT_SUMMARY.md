# AR封妖牌局 — PROJECT_SUMMARY（交接与项目状况汇总）

> **用途**：新对话 / 新同学 / 新 agent 接入时必读。  
> **更新日期**：2026-07-13（新增顺序抽牌逻辑 + 启动视频兼容性修复 + 真机手牌/出牌交互适配）  
> **策划案**：根目录 `AR封妖牌局_第一版玩法策划案.docx`  
> **UI 参考**：根目录 `UI设计参考图.png`  
> **重要美术资产**：
> - 铃铛图：`Assets/_ARSealCardGame/Art/UI/QTE/bell.png`
> - 卡面零件：`Assets/Prefabs/Cardpng/`（底/圆框/费用角/名称条 + 各符图案）
> - 手牌预制体：`Assets/Prefabs/Card/Card.prefab`（卡面已拼好；数据在 SO）

---

## 0. 新对话怎么开（复制即可）

### 0.1 通用（推荐）

```text
请先读 F:\AR-Card-Game\PROJECT_SUMMARY.md，重点看 §2 进度、§8 下一步、§15 新版 UI、§16 奖励与卡牌结构。
工程是 Unity 6 URP + Vuforia AR。主场景 Assets/Scenes/SampleScene.unity。
当前已打通三关解耦战役，集成了基于 FSM 的战斗流程、顺序抽牌、以及开始界面。
下一步任务优先：真机全流程物理扫图测试，或接入震动/音效与受击动画节奏微调。
```

---

## 1. 项目一句话

玩家在现实桌面扫图召唤妖怪，通过 **保留符咒、观察意图/弱点、打出对应符、完成 QTE** 进行回合制封妖。  
技术栈：**Unity 6 URP + Vuforia + Addressables + DOTween + TMP**。  
主场景：`Assets/Scenes/SampleScene.unity`。

---

## 2. 当前可玩进度（已实现情况）

| 模块 | 状态 | 说明 |
|------|------|------|
| **双相机手牌** | ✅ 已实现 | `ARCamera` (Base) + `CardCamera` (Overlay) 双相机配合，渲染于所有 UI 最前方，防 Vuforia 覆盖。 |
| **顺序抽牌动画** | ✅ 新增 | 玩家回合开始时，卡牌通过协程以 `0.4s` 间隔顺序飞入（方便未来接入抽牌音效）。已有手牌会立即平滑移动到新布局的位置，彻底解决了多张牌在抽牌期间堆叠拥挤的问题。 |
| **真机手牌越界适配** | ✅ 已修复 | 真机下 Vuforia 动态调整 FOV 会导致卡牌飞出屏幕。已重构为根据屏幕底部高度百分比（17.5% 处）动态计算世界坐标锚定手牌基准点。 |
| **出牌交互判定** | ✅ 已修复 | 抛弃了以往容易受真机 AR 空间追踪影响的绝对世界坐标 $Y$ 轴判定，防御与技能牌的“拖拽释放”改用屏幕高度比例（超过屏幕中线 `Screen.height * 0.5f`）判定。 |
| **开始界面/开场动画** | ✅ 已修复 | `PF_StartIntro` 支持 MP4 视频解码与 JPG 帧序列两种播放模式（避免低端设备解码 MP4 兼容性问题导致黑屏/卡顿），支持跳帧，淡出时使用黑白融合过渡，首帧与待机图一致以消除闪烁。 |
| **战斗 FSM 状态机** | ✅ 已重构 | 战斗流程完全通过 FSM 状态机运行：`BattleStateInit` -> `BattleStatePlayerTurn` -> `BattleStateEnemyTurn` -> `BattleStateEnd`。 |
| **三关战役 + 动态换模**| ✅ 已打通 | 解耦为 3 个独立 ImageTarget 扫卡触发对战，支持线性关卡解锁与通关逻辑。动态切换怪物模型（黄蜂怪 -> 爬行怪 -> 龙兽 Boss）。 |
| **符匣固定顺序** | ✅ 已实现 | 策划案 V1.0。菜单栏提供了 `AR封妖/按策划重置符匣与火符` 快捷恢复。 |
| **三色弱点 + QTE** | ✅ 已实现 | 三色弱点已在编辑器下永久精确锚定至怪物头部骨骼。隐藏了方块粒子，保留材质球呼吸灯与世界碰撞体。镇魂铃 QTE 为全屏压暗大铃，2 秒点 3 次。 |
| **伤害跳字/闪红/反馈**| ✅ 已实现 | 集成了 `DamagePopup`、`MonsterHitFlash` 及 `PlayerDamageFeedback`。怪物受击添加了 Exit Time 返回 Idle 的连线，解决了受击动画卡死导致的连续出牌延迟 Bug。 |
| **敌人意图** | ✅ 已实现 | 攻击/防御/蓄力意图仅显示单行名，底框大小随 preferred 宽高自适应收紧。 |
| **奖励三选一** | ✅ 已实现 | `RewardSelectUI` 临时挂在 `Screen Space - Camera`，**实例化真实的 `Card.prefab`**，而不是使用 UI 图片重拼，保证了细节完全一致。 |
| **灼烧与引爆构筑** | ✅ 已实现 | 烈火（灼烧 1）/ 炼火（灼烧 2），敌人回合结束结算，支持引火引爆。 |

**相对策划案仍缺**：精准圆环 QTE、Boss 多阶段、更丰富封印演出、受击音效/震动、真机全流程回归。

---

## 3. 战役与数据一览

### 3.1 战役流程

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
| 战役配置 | `Assets/Game Data/Stages/Campaign_Main.asset` |
| 关卡配置 | `Stage_01_XiaoYao` / `Stage_02_ShiLing` / `Stage_03_ShanGui` |
| 敌人配置 | `enemy_xiaoyao` / `enemy_shiling` / `enemy_shangui` |
| 符匣配置 | `Assets/Game Data/Card Library/FuXia_*.asset` |
| 卡牌数据 SO | `Assets/Game Data/Card Data/`（`attack`/`defense`/`break`/…/`reward_*`） |
| 手牌预制体 | `Assets/Prefabs/Card/Card.prefab` |
| 卡面零件+图案 | `Assets/Prefabs/Cardpng/` |
| 怪物 Prefab | `Assets/fbx/monsters/1|2|3/Prefabs/` |
| 中文字体 | `Assets/Scripts/Utilities/2.asset`、`3.asset`、`ziti.asset`（优先使用 2.asset 或 3.asset） |
| HUD 美术 | `Assets/Resources/BattleHudSkin/` |

### 3.3 固定补牌顺序（策划案 V1.0）

* **小妖**：开局 `斩斩护聚`；第 2 回合 `斩烈火`；第 3 回合 `护斩`（尾部填充破/镇）。  
* **石灵**：开局 `破斩护聚`；第 2 回合 `斩烈火`；第 3 回合 `护斩`；第 4 回合 `奖励1、斩`。  
* **山鬼**：开局 `镇斩护、奖励1`；第 2 回合 `破斩`；第 3 回合 `奖励2、聚`；第 4 回合 `烈火护`。  
* 奖励插入：配置于 `Stage_*.rewardInsertions`。可使用菜单栏恢复：`AR封妖/按策划重置符匣与火符`。

### 3.4 卡牌 id ↔ 中文名对应

| 文件名 | cardName | 类型 | 作用 |
|--------|----------|------|------|
| `attack` | 斩妖符 | Attack / 红 | 造成伤害 |
| `defense` | 护身符 | Defense | 叠甲 |
| `break` | 破煞符 | ArmorBreak / 黄 | 破甲/击破弱点 |
| `hp` | 聚气诀 | Ability | 恢复灵气 |
| `seal` | 镇魂符 | Seal / 紫 | 封印/弱点封妖 |
| `fire` | 烈火符 | Fire | 附加 1 层灼烧 |
| `reward_lianzhan` | 连斩符 | 奖励 Attack | 连击伤害 |
| `reward_lianhuo` | 炼火符 | 奖励 Fire | 附加 2 层灼烧 |
| `reward_zhenhunling` | 镇魂铃 | 奖励 Seal | 高额封印 |
| `reward_pozhen` | 破阵斩 | 奖励 Attack | 击破护甲伤害 |
| `reward_yinhuo` | 引火诀 | 奖励 引爆 | 引爆灼烧层数 |
| `reward_dinghun` | 定魂符 | 奖励 Seal | 强力封印 |

### 3.5 出牌方式

* **指向牌（箭头）**：红色斩妖符、黄色破煞符、紫色镇魂符、烈火符等。拖拽出箭头指向怪物头顶的弱点点位释放。
* **非指向牌（上拖）**：绿色护身符、聚气诀等。直接从手牌区拖过屏幕中线（`Screen.height * 0.5`）后松手释放。

---

## 4. 运行时架构说明

```
BattleBootstrap (控制何时开战，真机扫卡/Editor直接开战)
  → BattleFlowManager (驱动战役 Stage 载入，换模 ActiveMonster)
  → TurnManager (挂载战斗 FSM 状态机)
      ├─ BattleStateInit (灵气/数值初始化，通过 CardDeck 抽初始牌)
      ├─ BattleStatePlayerTurn (重置灵气，展现意图，DrawCardsOneByOneCoroutine 协程顺序抽牌)
      │    └─ 出牌交互 (CardDragHandler 射线碰撞弱点 / 拖过屏幕中线 -> 验证成功 -> QTE -> 触发数值与动效结算)
      ├─ BattleStateEnemyTurn (播放怪物攻击/重击动画，调用 EnemyIntentController 结算怪物伤害，回合末结算灼烧)
      └─ BattleStateEnd (关卡胜负判定)
           ├─ 胜利：RewardSelectUI.Show() (展示三选一卡牌，世界空间实例化 Card 实例) -> 选取后流转下一关
           └─ 失败：BattleResultUI 展示重试
```

---

## 5. 关键脚本

```text
Assets/Scripts/
  Campaign/
    BattleFlowManager.cs            # 战役流程控制器，负责多 ImageTarget 识别时的模型显隐与战役关卡切换
    ARStageTrigger.cs               # 配合 Vuforia ImageTarget 扫卡开战的触发桥梁
  Character/Mono/
    WeaknessPoint.cs                # 怪物身上的弱点逻辑组件，绑定头部骨骼
    WeaknessAnchorSetup.cs          # 编辑器弱点挂接工具
    EnemyIntentController.cs        # 怪物行动意图生成与轮换
    CharacterStats.cs               # 玩家/怪物数值组件（HP/甲/灵气/灼烧）
  Card/Mono/
    Card.cs                         # 卡牌主体逻辑（图案、费用、描述初始化）
    CardDeck.cs                     # 手牌队列容器，核心包含顺序抽牌协程与动态屏幕比例计算手牌基准点
    CardDragHandler.cs              # 拖拽与出牌检测（屏幕中线判断 & 射线检测弱点）
  Managers/
    CardManager.cs                  # 手牌实例对象池管理
    CardCameraManager.cs            # 动态管理 Card 渲染的 Overlay 相机堆栈
    TurnManager.cs                  # 回合管理者，集成 FSM 战斗状态机
    BattleBootstrap.cs              # 战斗启动器，支持真机扫卡开战和 Editor 模拟开战
  FSM/
    BattleStates.cs                 # 战斗状态机具体状态实现（Init/PlayerTurn/EnemyTurn/End）
  UI/
    BattleHudAdjustableController.cs # HUD UI 数据绑定与状态呈现
    BattleHudInfoPresenter.cs       # 意图文案（一行）、回合状态面板等文字内容刷新
    RewardSelectUI.cs               # 奖励三选一，使用真实 Card.prefab 挂载在 Overlay 渲染
    QTEPanelUI.cs                   # 镇魂铃 QTE 逻辑
    DamagePopup.cs                  # 伤害漂浮跳字
    PlayerDamageFeedback.cs         # 玩家受击屏幕泛红/震动（可拓展）反馈
  Utilities/
    TmpChineseFontUtil.cs           # 解决 TMP 缺字/字体动态绑定与材质更新工具

Assets/_ARSealCardGame/Scripts/
  UI/
    StartIntroController.cs         # 启动界面控制器，支持 MP4 与 Resources JPG 序列帧两种播放模式
```

---

## 6. 编辑器菜单快捷指令（AR封妖）

在 Unity 编辑器顶部菜单中提供了 `AR封妖` 子菜单，方便开发调试：

| 菜单项 | 作用 | 对应脚本 |
|------|------|------|
| `按策划重置符匣与火符` | 恢复策划案 V1.0 的符匣顺序、卡图命名和奖励卡牌插入。 | `ApplyPlanCardData.cs` |
| `配置奖励三选一卡牌界面` | 将 `Card.prefab` 动态绑定至主场景 `RewardSelectUI` 节点。 | `SetupRewardCardPresentation.cs` |
| `配置怪物Prefab弱点（头部）`| 精确自动将弱点物体挂载到三只怪物的头部骨骼中。 | `SetupMonsterPrefabWeaknesses.cs` |
| `重建镇魂铃QTE界面` | 重新生成/排布 QTE 面板的铃铛、遮罩和逻辑组件。 | `QTEPanelUI.cs` |
| `重建中文字体图集` | 使用中文字体 2/3 进行汉字重新烘焙，用于修补缺字导致的口口方框。 | `RebuildChineseFonts.cs` |
| `搭建可手动调整的战斗HUD` | **慎用**。若 HUD 发生严重错乱可跑此工具重建，但这会覆盖已精调的 HUD 布局。 | `BuildAdjustableBattleHudSkin.cs` |

---

## 7. 已知问题与避坑指南

### 7.1 弱点（已修复并优化）

* 弱点点位（红/黄/紫）已通过 `WeaknessAnchorSetup.ApplyForStage` 永久锚定到对应怪物的头部骨骼（`Vespomorph_Head` / `CAVECRAWLER_HEAD` / `Drackmahre_ Head`），已存盘场景，切勿改动头部骨骼命名。
* 弱点去除了红色 Billboard 方块粒子，仅使用呼吸材质与世界碰撞体，视觉更加干净。
* 运行时，未扫卡识别前不会显示怪物与弱点，防止穿模和视觉错乱。

### 7.2 UI / 渲染 / 避坑指南

1. **TMP 中文显示**：创建新的 TMP 文字必须使用中文字体资产 `2`，若发生更换，必须调用 `TmpChineseFontUtil.Apply` 以确保材质同步，若有缺字请通过编辑器菜单 `重建中文字体图集` 烘焙。
2. **非均匀 Scale 问题**：Boss 血条节点 `BossHealth_可调` 带有非均匀 Scale（如 $Y \approx 0.19$）。其子节点文字会被强行压扁。`BattleHudInfoPresenter` 会自动将 Boss 名字与意图文本 detach 并挂接至根节点以保证 Scale=1。添加新文本时注意避坑。
3. **意图文案格式**：意图只保留一行名称（如「普通攻击」），禁止塞入多行复杂文案或富文本 `<color>`。
4. **意图底框适配**：底框会跟随文字长度自动拉伸收紧（通过 `FitIntentBoxToText` 逻辑实现）。
5. **奖励卡渲染**：必须使用 `Instantiate(Card.prefab)` 并在运行时 `Card.Init` 传入数据。**禁止使用 UI 图像拼凑卡片**，否则由于渲染层级或卡片材质细节问题会导致奖励卡外观不对齐。展示奖励时，Canvas 会被临时修改为 `Screen Space - Camera` 以使 overlay 的 CardCamera 能够照到卡片，关闭后会自动切回 Overlay。
6. **顺序抽牌排列重叠问题**：顺序抽牌采用协程单张抽入（0.4s 间隔）。为保证整体手牌布局的平滑，当 `SetCardLayout` 被触发时，**已经存在于手牌中的卡牌（Scale > 0.01f）必须立刻且无延迟地平滑移往最新布局位置**；而仅有新抽出的卡片需要等待特定 delay 动效。如此可完美规避卡牌短暂堆叠在一起的视觉 bug。
7. **开始界面 Video 闪烁**：为了消除启动视频播放瞬间的“白屏/跳帧”，`StartIntroController` 将开始第一帧图片替换成了与待机完全一致的纹理，在视频播放结束后启动 `RevealArAfterTailHold`，通过透明度渐变与黑白过渡渐显 AR 相机。
8. **TurnManager 预设激活状态坑**：`TurnManager` 组件的 `isBattleActive` 在场景或预制体中必须默认为 `false`！否则扫卡时会被误判为“丢失识别重连”而拦截开战。我们在 `TurnManager.Awake` 中加入了强制初始化 `isBattleActive = false` 的兜底。

---

## 8. 下一步开发建议（按优先级）

### P0 核心物理与界面适配
* [ ] **真机全流程物理扫图测试**：在 Android/iOS 设备上进行实体扫图开战、关卡结算、奖励获取、关卡解锁全流程打通，排除 Vuforia 数据流异常。
* [ ] **UI 安全区适配**：特别是“结束回合”按钮及两边文字在全面屏/刘海屏下的安全边距适配，防止被圆角或遮罩裁切。

### P1 体验与手感微调
* [ ] **音效与受击震动接入**：
  - 在顺序抽牌时，随着卡片飞入每张牌触发一次“抽牌”音效；
  - 玩家受击时触发短时间的震动反馈；
  - 怪物打出伤害时，动画打击帧与 UI 扣血/伤害跳字节奏同步对齐。
* [ ] **拖拽卡牌时深度 Z 值优化**：
  - 目前手牌在 Z=4 渲染，拖拽时在 `CardDragHandler` 中被硬编码为了 Z=10，这会让卡牌在被拖拽的瞬间由于透视关系骤然缩得很小。建议将拖拽 Z 轴微调对齐，或者提供参数进行调节。

### P2 精修玩法
* [ ] **精准圆环 QTE / Boss 多阶段设计**。

---

## 9. 快速自测清单

1. **初始手牌**：开局手牌必为 `斩、斩、护、聚` 四张。
2. **回合开始抽牌**：玩家结束回合，怪物行动后，回到玩家回合。卡牌应每隔 0.4 秒一张张抽出，已有卡牌同时平滑移动，布局不发生挤压。
3. **出牌测试**：拖拽斩妖符能拉出红色箭头，对准怪物头部弱点松手能正常攻击；拖拽护身符向屏幕上方划过 50% 高度松手能正常叠甲。
4. **意图文案**：怪物意图为普通攻击等一行文案，文字无压扁，底框大小合适。
5. **关卡奖励**：击败第一关小妖后，画面压暗，显示三张实体卡牌尺寸相同的奖励卡，点击可正常选取并加入符匣。
6. **换模与关卡**：第一关为黄蜂怪，第二关为爬行怪（有甲），第三关为龙兽 Boss。
7. **启动界面**：点击八卦按钮后，墨迹视频平滑播放，平滑淡出显现战场，没有首帧闪白问题。

---

## 10. 本轮完成摘要 (2026-07-13)

| 项 | 结果 | 说明 |
|----|------|------|
| **顺序抽牌逻辑** | ✅ 已完成 | 移植了基于协程 `DrawCardsOneByOneCoroutine(interval=0.4s)` 的顺序抽牌逻辑，并重构了 `SetCardLayout` 中已有手牌立刻重排列的机制，消除了重叠堆靠 Bug。 |
| **真机越界与出牌适配** | ✅ 已完成 | 在 `CardDeck` 引入动态屏幕高度百分比（17.5%）确定手牌基准坐标。在 `CardDragHandler` 中改用屏幕高度中线（`Screen.height * 0.5f`）做出牌判定。 |
| **开始界面播放兼容性** | ✅ 已完成 | 补充了 Resources 帧序列 JPG 播放机制，解决 PC/Android 某些设备上解码 MP4 白屏/卡住问题，淡出融合过渡顺畅。 |
| **项目文档重整** | ✅ 已完成 | 对比了最新的 FSM 架构与抽牌、越界修复代码，将完全匹配的代码结构、运行逻辑以及已知避坑指南完整补充到 `PROJECT_SUMMARY.md` 中。 |
