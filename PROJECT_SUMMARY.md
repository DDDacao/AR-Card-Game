# AR封妖牌局 项目进度与架构总结 (PROJECT_SUMMARY)

本文件用于总结当前项目的架构、开发进度及下一步计划，方便在新对话中快速载入上下文。

---

## 📌 项目基本信息
* **开发环境**：Unity 6 (URP 通用渲染管线)
* **核心依赖**：Vuforia Engine (AR), Addressables, Input System, DOTween, TextMeshPro
* **玩法定位**：AR桌面卡牌封妖游戏（MStudio卡牌框架 + Vuforia AR实境结合）

---

## 🛠️ 当前已实现的功能 (截至 2026-07-10)

### 1. 卡牌核心框架 (Card System)
* **卡牌数据**：通过 `CardDataSO` 配置卡牌名称、费用、卡牌类型（攻击/防御/技能）及效果数值（伤害/护甲/回灵气）。
* **卡牌排布与发牌**：`CardDeck` 负责卡牌抽滤与发牌。`CardLayoutManager` 计算手牌在扇形/水平方向的 Local 位置，并由 DOTween 播放抽牌发牌动画。
* **本地化 HUD 绑定 (方案二)**：修改了 `Card.cs` 及 `CardDeck.cs` 内部定位逻辑为 `localPosition`。运行时卡牌会自动挂载至相机底部的子节点 `HandAnchor`，卡牌会永远锁定在屏幕底部并跟随相机移动，避免在 AR 空间下丢失。

### 2. 角色属性系统 (Character Stats)
* **属性组件**：`CharacterStats.cs` 挂载在隐藏的主角（空物体 `Player`）和怪物（`Ellen`）身上。
* **战斗结算**：
  * 支持伤害扣除、治疗。
  * **自带护甲抵扣逻辑**（受到伤害时优先扣除护甲，不够再扣除生命值）。
  * 提供了灵气（能量）的消耗 (`UseEnergy`) 和重置 (`ResetEnergy`) 机制。

### 3. 集中判定卡牌逻辑 (Card Execution)
* **统一结算**：`CardManager.Instance.ExecuteCard` 负责统一结算卡牌效果。
  * **攻击牌 (Attack)**：扣除敌方生命（扣除时先算护甲）。
  * **防御牌 (Defense)**：给玩家叠甲。
  * **技能牌 (Ability - 聚气诀)**：回复玩家 1 点灵气。
* **射线检测靶向 (Targeting)**：
  * `CardDragHandler.cs` 松手时，会自动从屏幕释放位置发射 **3D/2D 物理双重射线**。
  * 射中带有 `CharacterStats` 且非主角的敌人（Ellen）即判定打出成功。
  * 整合了**灵气（能量）扣减拦截**：灵气不足时出牌会自动弹性回弹复位。

### 4. 回合制循环系统 (Turn Loop)
* **回合控制器**：`TurnManager.cs` 控制战斗大循环。
  * **玩家回合**：玩家重置灵气，自动抽取 3 张牌。
  * **结束回合**：由 UI 的 `EndTurnButton` 点击触发。自动清空并回收所有剩余手牌，延迟 1.5 秒进入怪物回合。
  * **怪物回合**：自动获取场景敌人对 `Player` 造成 6 点伤害（可配），并清空怪物自身的护甲，延迟 1 秒后循环回玩家回合。
  * **回合出牌拦截**：非玩家回合时，卡牌拖拽会直接被拦截并报警告。

### 5. UI 血条与护甲同步 (UI System)
* **血条同步**：`HealthBarUI.cs` 挂载在血条 Canvas Slider 上，支持通过事件订阅自动更新血量数值、血条比例以及护甲数值。
* **护甲动态隐藏**：当护甲值为 0 时，护甲的 UI 容器会自动隐藏，非 0 时才会显现。
* **运行时颜色自适应**：代码在 `Start()` 时会自动将血条填充区（Fill）染为鲜红色，将背景（Background）染为暗灰色，免去了在编辑器里手动调色的繁琐操作。

---

## 🐛 已解决的重要 Bug
* **抽牌堆越界崩溃**：当抽牌堆空了还强行抽牌时报错。已在 `CardDeck.cs` 增加安全空判定拦截。
* **MainCamera 丢失报错**：替换 ARCamera 后导致 `Camera.main` 返回 null 崩溃。已在 `CardArrow.cs` 中增加空检测及提示，且项目已将 ARCamera 的 Tag 设为 `MainCamera`。
* **发牌函数访问权限报错**：`CardDeck.DrawCard()` 权限为 private 导致 `TurnManager` 无法调用。已改为 `public`。
* **双重抽牌与空手牌时序冲突**：因为脚本执行顺序随机，导致第一回合在卡组初始化完成前就抽牌导致空手牌。已将初始化权显式移交至 `TurnManager.Start` 中顺序执行。
* **API 过期警告**：Unity 6 全面废弃了旧版 `FindObject(s)OfType` 查找函数。已全部安全替换为 Unity 6 推荐的无参 `FindObjectsByType<T>()` 和 `FindAnyObjectByType<T>()`。

---

## 🚀 下一步开发计划 (TodoList)

1. **AR 识别与生成配置**：
   * 将场景中的 `Ellen_skin (1)` 拖入 `ImageTarget` 物体下作为子物体，用于真机识别扫描出现。
   * 确保 `HandAnchor` 物体是 `ARCamera` 的直接子物体，且其 Transform 的 `Position` 为 `(0, -2, 5)`（正 Z 轴代表相机前方，Z=-1 会导致卡牌刷在相机背后）。
2. **过渡到双相机叠加方案 (Camera Stacking) (推荐)**：
   * 为卡牌新建 Layer: `Card`。
   * 新建 `UICamera` 挂在 `ARCamera` 下，Render Type 设为 **Overlay**，Culling Mask 只勾选 `Card`。
   * 将 `ARCamera`（Base）的 Culling Mask 去掉 `Card`，并在最下方的 **Stack** 列表中把 `UICamera` 叠加上去。
   * 创建一个 `Canvas`（渲染模式设为 `Screen Space - Camera`，拖入 `UICamera` 绑定），在此 Canvas 下放置你的卡牌。这样卡牌能完美支持 `Canvas Scaler` 屏幕自适应，且绝不会和桌子上的 3D 怪物产生空间穿模。
3. **弱点与 QTE 机制**：
   * 在 Ellen 身上的特定关节（如胸口、头部）创建空 GameObject，加上 3D Collider，Layer 设为 `Weakness`。
   * 攻击牌指向释放射中 `Weakness` 后，触发限时 2 秒的点击 QTE 界面，点击满 3 次则造成额外破甲或双倍伤害，并打断怪物蓄力。

---

## 📂 关键脚本目录与对应关系
* **卡牌运行时**：[Card.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Card/Mono/Card.cs)
* **卡牌拖拽**：[CardDragHandler.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Card/Mono/CardDragHandler.cs)
* **卡组与发牌**：[CardDeck.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Card/Mono/CardDeck.cs)
* **指向箭头**：[CardArrow.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Card/Mono/CardArrow.cs)
* **属性系统**：[CharacterStats.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Character/Mono/CharacterStats.cs)
* **回合管理**：[TurnManager.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Managers/TurnManager.cs)
* **卡牌效果**：[CardManager.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Managers/CardManager.cs)
* **血条UI**：[HealthBarUI.cs](file:///c:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/UI/HealthBarUI.cs)
