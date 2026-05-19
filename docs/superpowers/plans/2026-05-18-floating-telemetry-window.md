# Floating Telemetry Window Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为桌面程序增加一个在设备连接后自动显示的始终置顶悬浮小窗，实时展示字/分钟、当前规则、当前波形和 A/B 通道强度。

**Architecture:** 采用“独立窗口 + 轻量状态同步”的方式实现。主窗口继续承担编辑和控制，小窗只负责观察，数据统一从 `AppBootstrapper` 已有事件流中同步，避免复制业务逻辑。

**Tech Stack:** C#、.NET 9、WPF、xUnit

---

## 文件结构

- Create: `src/KeyboardSpeed.Desktop/FloatingTelemetryWindow.xaml`
- Create: `src/KeyboardSpeed.Desktop/FloatingTelemetryWindow.xaml.cs`
- Create: `src/KeyboardSpeed.Desktop/Services/FloatingTelemetryPresenter.cs`
- Modify: `src/KeyboardSpeed.Desktop/App.xaml.cs`
- Modify: `src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs`
- Modify: `src/KeyboardSpeed.Desktop/MainWindow.xaml.cs`
- Test: `tests/KeyboardSpeed.Tests/Desktop/FloatingTelemetryWindowLayoutTests.cs`
- Test: `tests/KeyboardSpeed.Tests/Desktop/FloatingTelemetryPresenterTests.cs`

### Task 1: 补齐悬浮窗状态模型与测试

**Files:**
- Create: `src/KeyboardSpeed.Desktop/Services/FloatingTelemetryPresenter.cs`
- Test: `tests/KeyboardSpeed.Tests/Desktop/FloatingTelemetryPresenterTests.cs`

- [ ] **Step 1: 写失败测试，定义连接显示和文案输出**
- [ ] **Step 2: 运行测试确认失败**
- [ ] **Step 3: 实现最小状态同步逻辑**
- [ ] **Step 4: 运行测试确认通过**

### Task 2: 增加悬浮小窗 UI

**Files:**
- Create: `src/KeyboardSpeed.Desktop/FloatingTelemetryWindow.xaml`
- Create: `src/KeyboardSpeed.Desktop/FloatingTelemetryWindow.xaml.cs`
- Test: `tests/KeyboardSpeed.Tests/Desktop/FloatingTelemetryWindowLayoutTests.cs`

- [ ] **Step 1: 写失败测试，约束置顶、无边框和“字/分钟/A/B 通道”布局文案**
- [ ] **Step 2: 运行测试确认失败**
- [ ] **Step 3: 实现悬浮窗 XAML 和基础交互**
- [ ] **Step 4: 运行测试确认通过**

### Task 3: 接入应用生命周期和事件流

**Files:**
- Modify: `src/KeyboardSpeed.Desktop/App.xaml.cs`
- Modify: `src/KeyboardSpeed.Desktop/MainWindow.xaml.cs`
- Modify: `src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs`

- [ ] **Step 1: 让应用启动时创建悬浮窗实例**
- [ ] **Step 2: 把 `SnapshotUpdated` 和 `BluetoothStatusUpdated` 接到悬浮窗**
- [ ] **Step 3: 实现连接显示、断开隐藏**
- [ ] **Step 4: 运行桌面和测试验证主窗口不受影响**

### Task 4: 完整验证

**Files:**
- Modify: 如上

- [ ] **Step 1: 运行 `dotnet test tests\\KeyboardSpeed.Tests\\KeyboardSpeed.Tests.csproj`**
- [ ] **Step 2: 运行 `dotnet build KeyboardSpeed-YOKONEX.slnx`**
- [ ] **Step 3: 手动启动桌面程序，验证连接后小窗置顶显示**
- [ ] **Step 4: 按提交规范整理本轮改动**
