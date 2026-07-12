# 《AR 封妖牌局》手牌 Bug 修复与交接文档

> **用途**：由于上下文窗口限制，当您在 Unity 项目的新聊天窗口中重置对话时，请直接将本文件发送给新的 AI 助手，它将能够瞬间理解当前项目的修复进度、已修改的代码逻辑、以及下一步需要帮您确认的未尽事宜。

---

## 一、 本次对话中已解决的 Bug 汇总

### 1. 真机手牌越界/飞出屏幕底部不可见 Bug
* **数学根源**：
  在 Unity 编辑器中，默认相机 FOV 为 `60°`。但在真机手机上运行后，Vuforia 为了适应物理摄像头，会将 FOV 动态减小到约 `35° ~ 38°`。在深度 `Z=4` 处，视口底部的高度坐标从 `Y = -2.31` 收缩到 `Y = -1.375`。近期修改将卡牌偏移量 `centerPoint.y` 从 `-2.0f` 改为了 `-2.8f`，直接使得卡牌的绝对世界坐标（`-1.70f`）掉到了屏幕底部视口外。
* **修复方案**：
  在 [CardDeck.cs](Assets/Scripts/Card/Mono/CardDeck.cs) 的 `Update()` 方法中，将写死的位置映射修改为**动态屏幕百分比计算与反向位移补偿**：
  1. 通过 `ScreenToWorldPoint` 计算屏幕底部向上 `17.5%`（`0.175f`）高度处的绝对世界坐标作为卡牌目标点。
  2. 获取 `CardLayoutManager` 的局部偏移 `centerPoint.y` 与 `HandAnchor` 的缩放进行逆向补偿，动态设定 `HandAnchor` 的位置。
  这确保了不论在编辑器还是不同真机 FOV / 分辨率下，卡牌都恒定位于屏幕下方 17.5% 高度。

### 2. 真机上防御牌与技能牌（非指向性卡牌）拖出释放无效 Bug
* **逻辑根源**：
  代码原先使用绝对世界坐标判断出牌是否合法：`canExecute = worldPos.y > 0.5f;`。在编辑器中相机高度固定，拖拽卡牌轻易能超过 `0.5` 米；但在真机 AR 场景中，相机受 Vuforia 空间追踪，若手机初始化时或扫卡时相机绝对世界坐标极低（甚至为负数），无论怎么拖动卡牌，其世界坐标 Y 轴都无法超过 `0.5`，从而无法出牌。
* **修复方案**：
  在 [CardDragHandler.cs](Assets/Scripts/Card/Mono/CardDragHandler.cs) 的 `OnDrag` 和 `OnEndDrag` 中，抛弃绝对世界坐标判定，改用触屏指针的**屏幕高度比例**判定：
  `canExecute = eventData.position.y > Screen.height * 0.5f;`（即拖过屏幕中线 50% 松手即判定为成功出牌）。

---

## 二、 当前待继续解决/明确的问题（给新窗口助手的提示）

> **⚠️ 给新助手的指令**：用户在之前的对话中提出了问题 **“防御和技能牌拖拽出去后的距离在哪里改”**。我们已经将上文提到的出牌判定比例恢复为了原生的 `Screen.height * 0.5f`。请继续协助用户明确该“距离”的具体定义并提供修改方案。

### 潜在排查方向：

1. **拖拽时卡牌距离相机的深度（Z 轴距离）**：
   * **背景**：平时手牌在相机前 `Z = 4` 的位置（参见 [CardDeck.cs](Assets/Scripts/Card/Mono/CardDeck.cs) 的 `Update()`）。但在拖拽时，[CardDragHandler.cs](Assets/Scripts/Card/Mono/CardDragHandler.cs) 第 99 行硬编码转换的 Z 轴为 `10`：
     `Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);`
   * **现象**：当玩家拖拽卡牌时，卡牌会从深度 4 瞬间弹到深度 10，导致视觉上卡牌缩得很小，拖拽起来感觉距离极远。
   * **修复建议**：如果用户指的是这个“视觉距离/深度”，可引导用户将 `10` 调整为 `4`（与手牌对齐）或进行参数化。

2. **卡牌拖拽时移动的物理灵敏度/移动幅度**：
   * 在 `OnDrag` 中是通过 `dragCamera.ScreenToWorldPoint(screenPos)` 将屏幕鼠标位移转换成世界坐标，若由于相机视角或深度影响感觉移动距离不协调，可在此处调整。

3. **出牌判定比例高度**：
   * 判定防御/技能卡打出的屏幕比例高度（当前固定在 `Screen.height * 0.5f`）。

---

## 三、 本次修改的文件清单
* **[CardDeck.cs](Assets/Scripts/Card/Mono/CardDeck.cs)**：重构了 `Update()` 动态手牌挂接点计算。
* **[CardDragHandler.cs](Assets/Scripts/Card/Mono/CardDragHandler.cs)**：重构了 `OnDrag` / `OnEndDrag` 判定方式。
