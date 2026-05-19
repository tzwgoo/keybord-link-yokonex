# 悬浮遥测小窗设计文档

## 1. 目标

为当前桌面程序增加一个始终置顶的悬浮小窗，在蓝牙设备连接成功后自动显示，实时展示设备联动相关的核心信息，便于用户在切换到其他程序时仍能观察打字速度、当前波形和通道强度变化。

## 2. 范围

本次只增加“观察型”悬浮窗，不在小窗内放入复杂控制逻辑。

包含：

- 设备连接时自动显示
- 设备断开时自动隐藏
- 小窗始终置顶
- 实时波形预览
- A/B 通道强度显示
- 当前字/分钟显示
- 当前规则名称显示
- 当前波形名称显示
- 设备连接状态显示

不包含：

- 小窗内编辑波形
- 小窗内管理规则
- 小窗内蓝牙扫描或连接
- 多显示器停靠策略
- 自定义透明穿透

## 3. 设计方案

采用“独立置顶窗口 + 轻量状态同步”的方式实现。

- 新增 `FloatingTelemetryWindow`
  - 独立于主窗口存在
  - 无边框、轻量化、可拖动
  - `Topmost = true`
- 新增 `FloatingTelemetryPresenter`
  - 负责把 `AppBootstrapper` 当前的测速、规则、波形和蓝牙状态整理成适合悬浮窗消费的只读状态
  - 减少 `MainWindow` 和 `FloatingTelemetryWindow` 之间的直接耦合
- `App.xaml.cs`
  - 在应用启动后同时创建主窗口和悬浮窗
  - 负责应用退出时统一释放

## 4. 数据来源

悬浮窗只订阅现有两类事件：

- `AppBootstrapper.SnapshotUpdated`
- `AppBootstrapper.BluetoothStatusUpdated`

其中：

- 字/分钟来自 `TypingSpeedSnapshot.RealtimeKpm`
- 当前规则来自 `AppBootstrapper.CurrentRuleName`
- 当前波形来自 `AppBootstrapper.CurrentWaveformName`
- A/B 通道强度与连接状态来自 `BluetoothConnectionStatus`
- 波形预览优先显示当前已触发波形；若尚未触发，则显示最近选中或内置默认波形

## 5. 交互与显示行为

### 5.1 显示时机

- 程序启动且设备未连接：悬浮窗不显示
- 设备连接成功：悬浮窗自动显示
- 设备断开：悬浮窗自动隐藏

### 5.2 置顶行为

- 小窗始终保持 `Topmost = true`
- 不抢主窗口焦点
- 允许用户拖动到合适位置

### 5.3 布局内容

建议分为三层：

1. 头部状态
   - 连接设备名
   - 连接状态
2. 中部波形区
   - 当前波形名称
   - 实时波形预览
3. 底部指标区
   - 当前字/分钟
   - 当前规则
   - A 通道强度
   - B 通道强度

## 6. 错误处理

- 任一悬浮窗 UI 更新异常只记录日志，不影响主窗口
- 若波形预览数据为空，则显示占位文案，不抛异常
- 若蓝牙状态字段不完整，则显示 `--`

## 7. 测试策略

至少覆盖：

- 连接状态切换时的显示/隐藏逻辑
- 悬浮窗文案包含“字/分钟”和 A/B 通道信息
- 预览数据为空时的占位内容
- 小窗使用置顶窗口配置

## 8. 影响范围

主要修改文件预计包括：

- `src/KeyboardSpeed.Desktop/App.xaml.cs`
- `src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs`
- `src/KeyboardSpeed.Desktop/MainWindow.xaml.cs`
- 新增 `src/KeyboardSpeed.Desktop/FloatingTelemetryWindow.xaml`
- 新增 `src/KeyboardSpeed.Desktop/FloatingTelemetryWindow.xaml.cs`
- 新增 `src/KeyboardSpeed.Desktop/Services/FloatingTelemetryPresenter.cs`
- 新增相关测试

## 9. 2026-05-18 二次视觉调整

经确认，悬浮窗主视觉需要从“规则/波形信息卡”切换到“实时波形 + 实时强度”。

- 保留顶部设备名与连接状态
- 将字/分钟收纳为右上角辅助指标
- 中部改为大尺寸“实时波形”卡片，突出当前波形预览
- 底部改为“实时强度”卡片，突出 A/B 两路强度数值和条形反馈
- 原“当前规则”从主展示位移除，不再占据悬浮窗核心面积
