# 《AR封妖牌局》真机手牌越界/飞出屏幕 Bug 排查报告

> **用途**：当您在新开的聊天窗口中重置上下文时，请直接将本文件发送给新的 AI 助手，以使其能够无缝理解手牌越界问题的物理与数学根源，并进行精确排查。

---

## 一、 Bug 现象详尽总结
1. **测试现象**：
   * **在 Unity 编辑器中**：运行正常，手牌生成、发牌动画以及在底部的排列均可见，拖拽射线交互正常。
   * **在手机真机打包后**：开局对战扫卡成功后，卡牌首先在**屏幕中间偏下**的位置生成并放大（Scale 动画），随后播放手牌展开布局动画，卡牌**向下飞动并直接飞出屏幕底部边界**，最终停留在手机 UI 视口显示不到的地方，导致玩家看不到手牌，也无法进行出牌交互。
2. **用户强调线索**：
   * 之前的版本并没有出现这种“相机映射位置错误”的情况，正常情况下手牌应该停留在手机 UI 面板位置。
   * 极有可能是某一个近期版本中修改了场景（`SampleScene.unity`）或组件的某些关键参数，导致手牌基础位置或相机偏移出错。

---

## 二、 当前双相机与手牌渲染架构
* **主相机 (ARCamera)**：挂载 Vuforia 识别组件，渲染除卡牌以外的世界场景和怪物，对 `Card` 图层（Layer 6）进行剔除（Culling Mask 剔除）。
* **卡牌相机 (CardCamera)**：由 `CardCameraManager` 运行时动态创建并挂接为主相机子物体，设置渲染类型为 URP **Overlay（叠加）相机**，并将其加入主相机的 `cameraStack` 堆栈。它仅渲染 `Card` 图层，以便将 3D 卡牌渲染在所有 AR 画面和 UI 的最前方。
* **手牌挂接点 (HandAnchor)**：
  * 在场景 `SampleScene.unity` 中为 `ARCamera` 的子物体，静态局部坐标配置为 `(0, -0.5, 4)`，局部缩放为 `(0.5, 0.5, 0.5)`。
  * 手牌实例生成后，会被 `SetParent(handAnchor, false)` 挂载在其下，初始本地坐标为 `(0, 0, 0)`。
* **卡牌布局与偏移 (CardLayoutManager)**：
  * 卡牌的布局由 `CardLayoutManager` 计算，其配置的卡牌 Y 轴偏移量（`centerPoint.y`）为 `-2`。
  * 卡牌动画的终点为本地坐标 `(xPos, -2, 0)`。

---

## 三、 排查出的数学根源（为什么会飞出屏幕）

经过公式推导，定位了为什么“编辑器可见，真机飞出屏幕”：
1. **Vuforia 动态修改 FOV（视场角）**：
   * 在编辑器中，主相机 FOV 为默认的 `60°`。在相机前 `Z = 4` 的距离，视口底部的高度坐标是 `Y = -2.31`。
   * 在手机真机上，Vuforia 为了对齐物理摄像头传感器，会将相机的 FOV 动态改小（通常为 `35°` ~ `38°`）。如果 FOV 缩减为 `38°`，在 `Z = 4` 的深度，视口底部高度会急剧收缩为 `Y = -1.37`。
2. **固定物理偏移导致越界**：
   * 卡牌渲染时的最终坐标等于：`HandAnchor 局部高度 (-0.5)` + `LayoutManager 局部偏移 (-2) * 缩放 (0.5) = -1.5`。
   * 在编辑器中，卡牌 Y 坐标为 `-1.5`，大于视口底部 `-2.31`，所以可见。
   * 在真机上，卡牌 Y 坐标 `-1.5` **小于视口底部 `-1.37`**，因此卡牌在播放完发牌动画后，直接掉出了视口底线之外，彻底不可见。
3. **相机同步失效**：
   * `CardCamera` 的视口 `rect` 和 `projectionMatrix` 在运行时没有每帧与 `mainCamera` 同步。当 Vuforia 动态更新主相机的投影矩阵时，叠加相机的视角参数发生偏离，导致映射坐标失真。

---

## 四、 后续双端检查与验证方向（给新窗口助手的提示）

请引导新窗口的 AI 助手针对以下几点进行深入检查：

### 1. 历史参数核对（优先）
检查是否是最近的提交修改了 `SampleScene` 中 `HandAnchor` 的本地坐标，或者 `CardLayoutManager` 的 `centerPoint.y`。
* **正常参数期望值**：在旧版本中，手牌能够正常显示，需要核对当时 `HandAnchor` 的局部坐标是否比 `(0, -0.5, 4)` 更高，或者 `centerPoint.y` 的偏移比 `-2` 更小（例如 `-1`）。
* 可以通过 Git 历史命令排查参数变动：
  `git log -p -n 5 -- Assets/Scenes/SampleScene.unity`

### 2. 动态屏幕比例锚定修复（代码方案）
如果确定不是参数问题，必须使用动态计算代替静态写死坐标。计算公式应满足：
* 先根据屏幕的固定百分比（如屏幕底部向上 17.5% 处）和深度 `Z=4`，利用 `mainCam.ScreenToWorldPoint` 算出卡牌期望停留的绝对世界位置。
* 结合 `CardLayoutManager` 的局部偏移（默认 `-2f * 0.5f = -1.0f`）进行反向补偿，动态设置 `HandAnchor` 的世界位置：
  `handAnchor.position = cardTargetWorld - mainCam.transform.up * worldOffset;`

### 3. 相机防灾每帧强同步
在 [CardCameraManager.cs](file:///C:/Users/Dcao/Desktop/jpzw/1_AR_Team/demo1/demo1/Assets/Scripts/Managers/CardCameraManager.cs) 的 `LateUpdate` 中，每帧检查并确保 `mainCamera` 和 `cardCamera` 引用有效（若丢失则自动重新获取/重建堆栈），并强同步所有的投影和视口属性（`projectionMatrix`、`fieldOfView`、`aspect`、`rect` 等）。
