# AR封妖牌局 — PROJECT_SUMMARY（交接用）

> **用途**：新对话 / 新同学接入时先读本文件。  
> **更新日期**：2026-07-11（QTE 镇魂铃 / 受击反馈 / 弱点光球迭代中；**即将与同伴合并后再继续改弱点**）  
> **策划案**：根目录 `AR封妖牌局_第一版玩法策划案.docx`  
> **UI 参考**：根目录 `UI设计参考图.png`（横屏简易 HUD）  
> **铃铛图**：`Assets/_ARSealCardGame/Art/UI/QTE/bell.png`（由根目录 `铃铛.png` 导入）

---

## 0. 新对话怎么开（复制即可）

在新聊天中可以说：

```text
请先读 F:\AR-Card-Game\PROJECT_SUMMARY.md，从「## 下一步必须做什么」开始继续开发。
工程是 Unity 6 + Vuforia AR 卡牌。场景 SampleScene。可用菜单 AR封妖/* 一键重配。
当前优先：同伴合并后，继续打磨第 1 关小妖弱点（光球位置/粒子/瞄准反馈）。
```

若要做某一具体项，直接点名，例如：

- 「继续调小妖红弱点翅膀位置和 hitRadius」
- 「给第 2/3 关也做弱点锚点」
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
| 符匣固定顺序 | ✅ | `FuXiaOrderSO`，不洗牌；开局 4 / 每回补 2 / 上限 6；奖励按关卡节点插入 |
| 三色弱点 + QTE | ✅→🔧 | 逻辑可用；**视觉已改为柔光小球+粒子（进行中，见 §7.1 / §8）** |
| 镇魂铃 QTE UI | ✅ | 全屏压暗 + 居中大铃铛；2 秒点 3 次；第三击收定波纹；成功后 UI 关闭再结算 |
| 伤害跳字 | ✅ | `DamagePopup` 世界空间数字；QTE 成功金色更大 |
| 怪物弱点色闪 | ✅ | `MonsterHitFlash`：QTE 成功结算时身体淡红/黄/紫闪 |
| 玩家受击反馈 | ✅ | `PlayerDamageFeedback`：HUD 抖动 + 全屏红闪（电脑可测） |
| 敌人意图 | ✅ | 攻击→防御→蓄力；只亮对应弱点 |
| 横屏 HUD | ✅ | 顶敌人（名/血/意图/**独立灼烧行**）、右上玩家、右中提示、右下结束回合 |
| 三关战役 + Boss 模型 | ✅ | 运行时 `ActiveMonster` 换皮；编辑器仍见 Ellen 占位属正常 |
| 奖励三选一 | ✅ | 中文字体图集已补；优先字体 `2`/`3` |
| 灼烧构筑 | ✅ | 烈火 1 层 / 炼火 2 层；敌方行动后每层 1 伤；引火诀引爆 |
| 开始界面 | ✅ | 唯一 `PF_StartIntro`；动画完再开战 |
| 手机 AR | ✅ | 用户已验证扫图；Editor `skipARForEditor` |

**相对策划案仍缺**：精准 QTE、Boss 多阶段、更丰富封印演出、真机「未识别不开战」门闩（可选）、弱点三关精修、受击音效/手机震动。

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
| 符匣 | `Card Library/FuXia_XiaoYao|ShiLing|ShanGui.asset` |
| 基础牌 | `attack/defense/break/fire/seal/hp` |
| 奖励牌 | `reward_lianzhan/lianhuo/zhenhunling/pozhen/yinhuo/dinghun` |
| 玩家 | `player.asset`（50HP / 3 灵气） |
| 怪物预制体 | `fbx/monsters/1/Prefabs/Vespomorph`、`2/Cavecrawler`、`3/Drackmahre` |
| QTE 美术 | `Assets/_ARSealCardGame/Art/UI/QTE/bell.png`、`ripple_ring.png` |
| 中文字体 | `Assets/Scripts/Utilities/2.asset`、`3.asset`、`ziti.asset`（ziti fallback→2） |

### 3.3 固定补牌顺序（策划案 V1.0）

- **小妖**：开局 `斩、斩、护、聚`；第 2 回合 `斩、烈火`；第 3 回合 `护、斩`。
- **石灵**：开局 `破、斩、护、聚`；第 2 回合 `斩、烈火`；第 3 回合 `护、斩`；第 4 回合 `奖励 1、斩`。
- **山鬼**：开局 `镇、斩、护、奖励 1`；第 2 回合 `破、斩`；第 3 回合 `奖励 2、聚`；第 4 回合 `烈火、护`。

### 3.4 卡牌类型

`Attack` | `Defense` | `Ability` | `ArmorBreak` | `Seal` | `Fire`  
弱点：`RedAttack` / `YellowArmor` / `PurpleSeal`

### 3.5 指向牌 vs 上拖牌

- **箭头指向**：Attack / ArmorBreak / Seal / Fire  
- **上拖打出**：Defense / Ability  

---

## 4. 运行时架构（谁调用谁）

```
BattleBootstrap.BeginBattle()
  └─ BattleFlowManager.StartCampaign()
       └─ ApplyStageToBattle
            ├─ SwapMonsterModel → ActiveMonster 实例化
            ├─ WeaknessAnchorSetup.ApplyForStage  // 弱点锚骨骼（先做了第1关）
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
```

---

## 5. 关键脚本地图（新增标 ⭐）

```
Assets/Scripts/
  Campaign/
    BattleFlowManager.cs      # 三关 + 换模 + 调 WeaknessAnchorSetup
  Character/Mono/
    WeaknessPoint.cs          # ⭐ 光芯+粒子+大判定+瞄准高亮+跟随骨骼
    WeaknessAnchorSetup.cs    # ⭐ 第1关红弱点绑 Vespomorph 右翅
    CharacterStats.cs         # OnTookHit 受击事件
    EnemyIntentController.cs
    MonsterAnimationBridge.cs
  Combat/
    QTEManager.cs             # 成功回调延后到面板隐藏后
  Card/Mono/
    CardDragHandler.cs        # ⭐ 拖动时弱点瞄准反馈
  UI/
    QTEPanelUI.cs             # 镇魂铃抖动/波纹/第三击收定
    DamagePopup.cs            # ⭐ 伤害跳字
    MonsterHitFlash.cs        # ⭐ QTE 成功身体闪色
    PlayerDamageFeedback.cs   # ⭐ 玩家受击 HUD 抖+红闪
    BattleInfoUI.cs           # ⭐ 灼烧独立行（勿把 <color> 塞意图文本）
    RewardSelectUI.cs
  Utilities/
    TmpChineseFontUtil.cs     # ⭐ 动态补字；优先字体 2/3
  Managers/
    TurnManager.cs / CardManager.cs / BattleBootstrap.cs

Assets/Editor/
  SetupWeaknessAndQTE.cs      # 重建镇魂铃 QTE：AR封妖/重建镇魂铃QTE界面
  BuildBattleHud.cs           # + AR封妖/修复敌人状态UI布局
  RebuildChineseFonts.cs      # AR封妖/重建中文字体图集（修复乱码）
```

---

## 6. Unity 编辑器菜单（AR封妖）

| 菜单 | 作用 |
|------|------|
| `AR封妖/重建战斗HUD（横屏）` | 整套 HUD（慎用，会清子物体） |
| `AR封妖/修复敌人状态UI布局` | 只加高敌人顶栏 + 独立灼烧行 |
| `AR封妖/配置弱点与QTE` | 弱点+QTE（会清弱点，慎用） |
| `AR封妖/重建镇魂铃QTE界面` | **只重建 QTE**，不碰弱点 |
| `AR封妖/配置多弱点与新符咒` | 三色弱点 |
| `AR封妖/配置三关战役` | 战役 SO + BattleFlowManager |
| `AR封妖/重建中文字体图集（修复乱码）` | 把玩法汉字烘焙进 TMP 2/3/ziti |

**场景应有**：`ARCamera`、`ImageTarget`、`Ellen_skin (2)`（逻辑宿主 + 三 WeaknessPoint + Intent）、`BattleFlowManager`（三预制体引用）、`Canvas`（HUD_* / HUD_QTE / HUD_Reward / HUD_DamageFlash 运行时生成）、`QTEManager`。

**编辑器 vs 真机模型**：场景里永久是 Ellen 占位；Play/真机开战由 `SwapMonsterModel` 挂 `ActiveMonster`。属设计如此，不是漏换模。

---

## 7. 已知问题 / 注意点

### 7.1 弱点（进行中 — 合并后再继续）

1. **已做**：`WeaknessPoint` 小光芯 + 粒子；**大 `hitRadius`（约 0.62–0.68）** 易点中；拖牌瞄准 `SetAimed` 变亮；第 1 关红弱点尝试跟随 `Vespomorph_WingRightA`。  
2. **用户反馈仍有问题**（合并后优先修）：位置/亮度/粒子/跟随偏移等需再调；黄/紫与第 2/3 关锚点未精修。  
3. 调参入口：  
   - `WeaknessPoint.visualCoreScale` / `hitRadius` / `glowParticleSize`  
   - `WeaknessAnchorSetup.SetupXiaoYao` 里 `BindFollow(wing, localOffset)`  
4. 射线：`CardDragHandler.ResolveAttackTarget` 必须 `RaycastAll` 优先弱点。

### 7.2 其它注意

5. **QTE 成功结算时机**：结果 UI 关闭后才跳字/扣血/闪色（`QTEPanelUI.OnHiddenAfterResult`）。  
6. **QTE 首次显示**：`QTEPanelUI.Awake` 禁止 `HideImmediate`（否则第一次不弹面板）。  
7. **敌人状态灼烧**：必须用独立 `enemyBurnText`，禁止 `intent + "<color>…"` 字符串（会显示标签原文）。  
8. **中文乱码**：TMP 图集缺字；跑「重建中文字体图集」；优先用字体资产 `2`/`3`；`ziti` 已 fallback 到 `2`。  
9. **红闪 Image 必须有 Sprite**：`PlayerDamageFeedback` 用白贴图生成 Sprite，否则电脑上看不见闪。  
10. **模型**：三预制体序列化在 `BattleFlowManager`；禁止改回仅 Editor 的 AssetDatabase 加载。  
11. **开始界面**：仅一个 `PF_StartIntro`。  
12. **灼烧**：敌方行动后结算；引火诀引爆清层。

---

## 8. 下一步必须做什么（按优先级）

### P0 — 合并后立刻继续（当前主线）

**弱点打磨（用户明确：合并后再改）**

- [ ] 与同伴合并分支，解决冲突后回归 Play  
- [ ] 第 1 关红弱点：翅膀位置、`followLocalOffset`、粒子强度、瞄准反馈手感  
- [ ] 确认「看起来小、判定大」在真机/电脑都好点  
- [ ] 通过后：黄/紫锚点；第 2 关石灵 / 第 3 关山鬼 `WeaknessAnchorSetup`  

### P0 — 发布向

**A. 真机全流程回归**

- [ ] 扫图 → 三关 → 两次奖励 → 全通 / 失败重试  
- [ ] 弱点位置、UI 安全区、结束回合  
- [ ] 三关模型与受击/死亡动画  

### P1 — 表现

- [ ] 玩家受击：手机 `Handheld.Vibrate`（可选）  
- [ ] QTE 音效；重击碎屏（可选）  
- [ ] 怪物攻击/受击/死亡动画对齐战斗事件  

### P2 — 策划剩余

- [ ] 精准圆环 QTE  
- [ ] 连斩条件效果  
- [ ] Boss 多阶段  

---

## 9. 建议的下一条任务（直接开干用）

**首选（合并完成后）：**

> 继续优化弱点：在第 1 关小妖上调红弱点翅膀附着点与 hitRadius/光效；拖斩妖符瞄准时应明显变亮；确认 QTE 仍易触发。满意后再做黄/紫与其它关。

**次选（发布）：**

> 真机全流程回归 + 记录问题清单。

**再次选：**

> 玩家受击加短震动；QTE 成功/失败音效。

---

## 10. 快速自测清单（改完必过）

1. 开始界面一次点击 → 动画完进第 1 关；开局 4 张序正确。  
2. 小妖模型为 Vespomorph；红弱点在翅膀附近（非巨大粗糙球）。  
3. 拖斩妖符对准弱点：光球变亮 → 松手 QTE 铃铛 → 成功后 UI 消失再跳字/闪色。  
4. 结束回合若敌人攻击：HUD 抖 + 红闪。  
5. 叠灼烧后顶栏独立显示「灼烧 n 层」，无 `<color=` 字样。  
6. 奖励三选一中文无大量方框。  
7. 三关切换模型；失败重试；全通。  
8. 烈火/炼火/引火灼烧逻辑正确。

---

## 11. 技术备忘

* Unity 6：`FindObjectsByType` / `FindAnyObjectByType`。  
* Grok + Unity MCP：`http://localhost:8080/mcp`；Unity 开 MCP Bridge。  
* DOTween：UI `DOShakeAnchorPos`、`Image.DOColor`（Image 需 Sprite）。  
* TMP 动态字体：`atlasPopulationMode=Dynamic` + `isMultiAtlasTexturesEnabled`；毛笔字体本身可能缺字，靠 fallback。

---

## 12. Git / 合并说明

### 12.1 历史合并（Boss 模型，2026-07-11）

- 分支 `codex/pre-boss-merge-20260711` 曾合并 `origin/master`（0.4）。  
- 合并提交参考：`8b2e89f`；此前安全点：`0ae1457`。  

### 12.2 当前状态（请用户本地确认）

- 本轮新增大量表现向改动（QTE 铃铛、跳字、受击反馈、弱点光球、字体工具、HUD 灼烧行等），**用户准备与同伴再合并**。  
- **合并后再继续改弱点**——不要在冲突未解决时大幅重调 `WeaknessAnchorSetup` 坐标。  
- 合并时易冲突文件（注意保留双方意图）：  
  - `PROJECT_SUMMARY.md`（以本文件为功能真相源，合并后可再对一次）  
  - `SampleScene.unity`（HUD / QTE / 弱点引用）  
  - `BattleFlowManager.cs`、`CardDragHandler.cs`、`WeaknessPoint.cs`  
  - `QTEPanelUI.cs` / `QTEManager.cs` / `CharacterStats.cs`  
  - 字体 `2.asset` / `3.asset` / `ziti.asset`（图集体积大，慎用双方各生成一版硬拼）  

---

## 13. 本轮会话已完成摘要（2026-07-11 表现迭代）

| 项 | 结果 |
|----|------|
| QTE 首次不弹 UI | 修：`Awake` 勿 `HideImmediate` |
| 镇魂铃 QTE | 居中大铃 + 压暗 0.78 + 抖/波纹/第三击收定 |
| 成功结算节奏 | UI 消失后再跳字 + 弱点色闪 |
| 敌人灼烧 UI | 独立行，修掉富文本标签漏出 |
| 中文乱码 | 重建图集 + `TmpChineseFontUtil` + ziti→2 fallback |
| 玩家受击 | HUD 抖 + 红闪（修无 Sprite 不显示） |
| 弱点 V1 | 光球+粒子+大判定+瞄准高亮+小妖红翅跟随 |
| 弱点 V1 评价 | **用户：还有问题，合并后再改** |

---

**交接结论**：玩法闭环可演示；本轮重点在 **QTE/反馈/字体/弱点视觉**。  
**新聊天优先**：① 同伴合并收尾 ② **继续打磨第 1 关弱点** ③ 真机回归。  
开场请读 **§0 / §7.1 / §8 / §9**。
