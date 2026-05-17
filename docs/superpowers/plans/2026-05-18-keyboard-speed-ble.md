# Keyboard Speed BLE Desktop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Windows 桌面程序，支持全局键盘测速、蓝牙 EMS 设备接入、自定义波形编辑、波形图展示，以及按速度区间自动触发波形。

**Architecture:** 采用 `WPF UI + Core 纯业务层 + Windows 输入适配层 + Windows BLE 适配层` 的分层结构。输入监听、速度统计、规则命中、波形调度和 BLE 发包彼此解耦，便于测试和后续替换协议或设备实现。

**Tech Stack:** C#、.NET 9、WPF、Windows BLE API、System.Text.Json、xUnit

---

## 文件结构

### 解决方案与项目

- Create: `KeyboardSpeed-YOKONEX.sln`
- Create: `.gitignore`
- Create: `Directory.Build.props`
- Create: `src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj`
- Create: `src/KeyboardSpeed.Input.Windows/KeyboardSpeed.Input.Windows.csproj`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/KeyboardSpeed.Bluetooth.Windows.csproj`
- Create: `src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj`
- Create: `tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj`

### Core 项目

- Create: `src/KeyboardSpeed.Core/Configuration/AppSettings.cs`
- Create: `src/KeyboardSpeed.Core/Configuration/SettingsJsonContext.cs`
- Create: `src/KeyboardSpeed.Core/Configuration/SettingsStore.cs`
- Create: `src/KeyboardSpeed.Core/Typing/KeystrokeSample.cs`
- Create: `src/KeyboardSpeed.Core/Typing/TypingSpeedSnapshot.cs`
- Create: `src/KeyboardSpeed.Core/Typing/TypingSpeedOptions.cs`
- Create: `src/KeyboardSpeed.Core/Typing/TypingSpeedCalculator.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedMetricType.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedRangeRule.cs`
- Create: `src/KeyboardSpeed.Core/Rules/RuleMatchResult.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedRuleEngine.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedRuleCoordinator.cs`
- Create: `src/KeyboardSpeed.Core/Waveforms/EmsWaveformDefinition.cs`
- Create: `src/KeyboardSpeed.Core/Waveforms/EmsWaveformStep.cs`
- Create: `src/KeyboardSpeed.Core/Waveforms/BuiltinWaveforms.cs`
- Create: `src/KeyboardSpeed.Core/Bluetooth/BluetoothDeviceDescriptor.cs`
- Create: `src/KeyboardSpeed.Core/Bluetooth/BluetoothConnectionStatus.cs`
- Create: `src/KeyboardSpeed.Core/Bluetooth/BluetoothTelemetrySample.cs`
- Create: `src/KeyboardSpeed.Core/Bluetooth/BluetoothTelemetrySnapshot.cs`
- Create: `src/KeyboardSpeed.Core/Bluetooth/IBluetoothDeviceManager.cs`

### Input 项目

- Create: `src/KeyboardSpeed.Input.Windows/Interop/NativeMethods.cs`
- Create: `src/KeyboardSpeed.Input.Windows/GlobalKeyboardHook.cs`
- Create: `src/KeyboardSpeed.Input.Windows/GlobalKeyboardListener.cs`

### Bluetooth 项目

- Create: `src/KeyboardSpeed.Bluetooth.Windows/Protocol/EmsPacketChecksum.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Protocol/EmsBleProtocolAdapter.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/IWindowsBlePlatformBridge.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/WindowsBlePlatformBridge.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/BluetoothTelemetryStore.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/BleDeviceManager.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/BluetoothNotificationParser.cs`

### Desktop 项目

- Create: `src/KeyboardSpeed.Desktop/App.xaml`
- Create: `src/KeyboardSpeed.Desktop/App.xaml.cs`
- Create: `src/KeyboardSpeed.Desktop/MainWindow.xaml`
- Create: `src/KeyboardSpeed.Desktop/MainWindow.xaml.cs`
- Create: `src/KeyboardSpeed.Desktop/Resources/Colors.xaml`
- Create: `src/KeyboardSpeed.Desktop/Resources/Typography.xaml`
- Create: `src/KeyboardSpeed.Desktop/Resources/Controls.xaml`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/ObservableObject.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/RelayCommand.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/MainViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/OverviewViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/DevicesViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/WaveformsViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/RulesViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/Views/OverviewView.xaml`
- Create: `src/KeyboardSpeed.Desktop/Views/DevicesView.xaml`
- Create: `src/KeyboardSpeed.Desktop/Views/WaveformsView.xaml`
- Create: `src/KeyboardSpeed.Desktop/Views/RulesView.xaml`
- Create: `src/KeyboardSpeed.Desktop/Controls/TrendChartControl.cs`
- Create: `src/KeyboardSpeed.Desktop/Controls/WaveformPreviewControl.cs`
- Create: `src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs`

### 测试项目

- Create: `tests/KeyboardSpeed.Tests/Typing/TypingSpeedCalculatorTests.cs`
- Create: `tests/KeyboardSpeed.Tests/Rules/SpeedRuleEngineTests.cs`
- Create: `tests/KeyboardSpeed.Tests/Rules/SpeedRuleCoordinatorTests.cs`
- Create: `tests/KeyboardSpeed.Tests/Waveforms/BuiltinWaveformsTests.cs`
- Create: `tests/KeyboardSpeed.Tests/Bluetooth/EmsBleProtocolAdapterTests.cs`
- Create: `tests/KeyboardSpeed.Tests/Configuration/SettingsStoreTests.cs`

## Task 1: 初始化解决方案与仓库基础结构

**Files:**
- Create: `KeyboardSpeed-YOKONEX.sln`
- Create: `.gitignore`
- Create: `Directory.Build.props`
- Create: `src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj`
- Create: `src/KeyboardSpeed.Input.Windows/KeyboardSpeed.Input.Windows.csproj`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/KeyboardSpeed.Bluetooth.Windows.csproj`
- Create: `src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj`
- Create: `tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj`

- [ ] **Step 1: 创建解决方案和项目骨架**

Run:

```powershell
dotnet new sln -n KeyboardSpeed-YOKONEX
dotnet new classlib -n KeyboardSpeed.Core -o src/KeyboardSpeed.Core
dotnet new classlib -n KeyboardSpeed.Input.Windows -o src/KeyboardSpeed.Input.Windows
dotnet new classlib -n KeyboardSpeed.Bluetooth.Windows -o src/KeyboardSpeed.Bluetooth.Windows
dotnet new wpf -n KeyboardSpeed.Desktop -o src/KeyboardSpeed.Desktop
dotnet new xunit -n KeyboardSpeed.Tests -o tests/KeyboardSpeed.Tests
```

- [ ] **Step 2: 把项目加入解决方案**

Run:

```powershell
dotnet sln KeyboardSpeed-YOKONEX.sln add src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj
dotnet sln KeyboardSpeed-YOKONEX.sln add src/KeyboardSpeed.Input.Windows/KeyboardSpeed.Input.Windows.csproj
dotnet sln KeyboardSpeed-YOKONEX.sln add src/KeyboardSpeed.Bluetooth.Windows/KeyboardSpeed.Bluetooth.Windows.csproj
dotnet sln KeyboardSpeed-YOKONEX.sln add src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
dotnet sln KeyboardSpeed-YOKONEX.sln add tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj
```

- [ ] **Step 3: 配置目标框架、引用和统一构建属性**

在 `Directory.Build.props` 中统一设置：

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

项目目标建议：

- `KeyboardSpeed.Core`: `net9.0`
- `KeyboardSpeed.Input.Windows`: `net9.0-windows10.0.19041.0`
- `KeyboardSpeed.Bluetooth.Windows`: `net9.0-windows10.0.19041.0`
- `KeyboardSpeed.Desktop`: `net9.0-windows10.0.19041.0`
- `KeyboardSpeed.Tests`: `net9.0-windows10.0.19041.0`

- [ ] **Step 4: 添加项目引用**

Run:

```powershell
dotnet add src/KeyboardSpeed.Input.Windows/KeyboardSpeed.Input.Windows.csproj reference src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj
dotnet add src/KeyboardSpeed.Bluetooth.Windows/KeyboardSpeed.Bluetooth.Windows.csproj reference src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj
dotnet add src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj reference src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj
dotnet add src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj reference src/KeyboardSpeed.Input.Windows/KeyboardSpeed.Input.Windows.csproj
dotnet add src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj reference src/KeyboardSpeed.Bluetooth.Windows/KeyboardSpeed.Bluetooth.Windows.csproj
dotnet add tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj reference src/KeyboardSpeed.Core/KeyboardSpeed.Core.csproj
dotnet add tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj reference src/KeyboardSpeed.Bluetooth.Windows/KeyboardSpeed.Bluetooth.Windows.csproj
```

- [ ] **Step 5: 运行一次基线构建**

Run:

```powershell
dotnet build KeyboardSpeed-YOKONEX.sln
```

Expected: 所有项目成功编译，只有模板文件存在，无业务代码失败。

- [ ] **Step 6: 提交骨架**

Run:

```powershell
git add .
git commit -m "chore(项目骨架): 初始化键盘测速蓝牙联动程序解决方案"
```

## Task 2: 建立核心配置、波形和规则模型

**Files:**
- Create: `src/KeyboardSpeed.Core/Configuration/AppSettings.cs`
- Create: `src/KeyboardSpeed.Core/Configuration/SettingsJsonContext.cs`
- Create: `src/KeyboardSpeed.Core/Configuration/SettingsStore.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedMetricType.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedRangeRule.cs`
- Create: `src/KeyboardSpeed.Core/Waveforms/EmsWaveformDefinition.cs`
- Create: `src/KeyboardSpeed.Core/Waveforms/EmsWaveformStep.cs`
- Create: `src/KeyboardSpeed.Core/Waveforms/BuiltinWaveforms.cs`
- Test: `tests/KeyboardSpeed.Tests/Waveforms/BuiltinWaveformsTests.cs`
- Test: `tests/KeyboardSpeed.Tests/Configuration/SettingsStoreTests.cs`

- [ ] **Step 1: 写内置波形和配置存储的失败测试**

```csharp
[Fact]
public void BuiltinWaveforms_ShouldIncludeHeartbeatPreset()
{
    var waveforms = BuiltinWaveforms.CreateDefaults();
    Assert.Contains(waveforms, x => x.Name == "Heartbeat");
}

[Fact]
public async Task SettingsStore_ShouldRoundTripRulesAndWaveforms()
{
    var store = new SettingsStore(path);
    await store.SaveAsync(settings, CancellationToken.None);
    var loaded = await store.LoadAsync(CancellationToken.None);
    Assert.Equal(settings.SpeedRules.Count, loaded.SpeedRules.Count);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~BuiltinWaveformsTests|FullyQualifiedName~SettingsStoreTests"
```

Expected: FAIL，提示类型或方法尚未实现。

- [ ] **Step 3: 实现波形、规则和配置模型**

核心模型至少包含：

```csharp
public sealed record EmsWaveformStep(
    int DurationMs,
    int AStrength,
    int AMode,
    int BStrength,
    int BMode,
    int MotorState);

public sealed record SpeedRangeRule(
    string Id,
    string Name,
    SpeedMetricType MetricType,
    double MinValue,
    double MaxValue,
    string WaveformId,
    int CooldownMs,
    bool Enabled,
    bool TriggerOnEnter,
    bool RepeatWithinRange,
    bool StopOnExit);
```

- [ ] **Step 4: 实现 JSON 配置读写**

配置文件路径在桌面层传入，Core 层只负责读写和默认值兜底：

```csharp
public sealed class SettingsStore
{
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: 运行测试确认通过**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~BuiltinWaveformsTests|FullyQualifiedName~SettingsStoreTests"
```

Expected: PASS

- [ ] **Step 6: 提交核心模型**

Run:

```powershell
git add src/KeyboardSpeed.Core tests/KeyboardSpeed.Tests
git commit -m "feat(核心模型): 增加配置波形与速度区间规则定义"
```

## Task 3: 实现打字速度统计器

**Files:**
- Create: `src/KeyboardSpeed.Core/Typing/KeystrokeSample.cs`
- Create: `src/KeyboardSpeed.Core/Typing/TypingSpeedSnapshot.cs`
- Create: `src/KeyboardSpeed.Core/Typing/TypingSpeedOptions.cs`
- Create: `src/KeyboardSpeed.Core/Typing/TypingSpeedCalculator.cs`
- Test: `tests/KeyboardSpeed.Tests/Typing/TypingSpeedCalculatorTests.cs`

- [ ] **Step 1: 写失败测试，覆盖滑动窗口和指标换算**

```csharp
[Fact]
public void Calculator_ShouldUseRecentTenSecondWindowForRealtimeKpm()
{
    var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());
    var now = DateTimeOffset.Parse("2026-05-18T10:00:10+08:00");
    calculator.RecordKeystroke(now.AddSeconds(-9));
    calculator.RecordKeystroke(now.AddSeconds(-4));
    var snapshot = calculator.CreateSnapshot(now);
    Assert.Equal(12d, snapshot.RealtimeKpm, 1);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter FullyQualifiedName~TypingSpeedCalculatorTests
```

Expected: FAIL

- [ ] **Step 3: 实现速度统计器**

实现约束：

- 10 秒窗口输出实时 KPM/WPM
- 30 秒窗口输出趋势 KPM/WPM
- 自动裁剪过期样本
- 忽略空输入

最小接口：

```csharp
public sealed class TypingSpeedCalculator
{
    public void RecordKeystroke(DateTimeOffset timestamp);
    public TypingSpeedSnapshot CreateSnapshot(DateTimeOffset now);
}
```

- [ ] **Step 4: 补齐边界测试**

新增场景：

- 0 输入时返回 0
- 过期样本被剔除
- WPM 按 `KPM / 5` 换算

- [ ] **Step 5: 运行测试确认通过**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter FullyQualifiedName~TypingSpeedCalculatorTests
```

Expected: PASS

- [ ] **Step 6: 提交速度统计器**

Run:

```powershell
git add src/KeyboardSpeed.Core/Typing tests/KeyboardSpeed.Tests/Typing
git commit -m "feat(测速核心): 实现打字速度滑动窗口统计器"
```

## Task 4: 实现速度区间规则引擎与调度器

**Files:**
- Create: `src/KeyboardSpeed.Core/Rules/RuleMatchResult.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedRuleEngine.cs`
- Create: `src/KeyboardSpeed.Core/Rules/SpeedRuleCoordinator.cs`
- Test: `tests/KeyboardSpeed.Tests/Rules/SpeedRuleEngineTests.cs`
- Test: `tests/KeyboardSpeed.Tests/Rules/SpeedRuleCoordinatorTests.cs`

- [ ] **Step 1: 写失败测试，覆盖区间命中和冷却逻辑**

```csharp
[Fact]
public void Engine_ShouldMatchMiddleRuleForCurrentKpm()
{
    var result = engine.Match(snapshot, rules, now);
    Assert.Equal("mid", result.ActiveRule?.Id);
}

[Fact]
public void Coordinator_ShouldNotRetriggerWithinCooldown()
{
    var first = coordinator.Evaluate(snapshot, rules, now);
    var second = coordinator.Evaluate(snapshot, rules, now.AddMilliseconds(300));
    Assert.True(first.ShouldDispatch);
    Assert.False(second.ShouldDispatch);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~SpeedRuleEngineTests|FullyQualifiedName~SpeedRuleCoordinatorTests"
```

Expected: FAIL

- [ ] **Step 3: 实现规则命中与状态记忆**

核心能力：

- 根据 `KPM/WPM` 选中当前区间
- 记住当前激活规则
- 在区间切换时返回需要播放的新波形
- 离开区间并开启 `StopOnExit` 时返回停止动作

- [ ] **Step 4: 补齐重复触发与边界值测试**

补充场景：

- `MinValue`/`MaxValue` 边界包含策略
- 禁用规则不参与命中
- 区间内允许重复触发时按冷却周期触发

- [ ] **Step 5: 运行测试确认通过**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~SpeedRuleEngineTests|FullyQualifiedName~SpeedRuleCoordinatorTests"
```

Expected: PASS

- [ ] **Step 6: 提交规则引擎**

Run:

```powershell
git add src/KeyboardSpeed.Core/Rules tests/KeyboardSpeed.Tests/Rules
git commit -m "feat(规则引擎): 支持按速度区间匹配波形触发规则"
```

## Task 5: 接入 Windows 全局键盘监听

**Files:**
- Create: `src/KeyboardSpeed.Input.Windows/Interop/NativeMethods.cs`
- Create: `src/KeyboardSpeed.Input.Windows/GlobalKeyboardHook.cs`
- Create: `src/KeyboardSpeed.Input.Windows/GlobalKeyboardListener.cs`
- Modify: `src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs`

- [ ] **Step 1: 先定义监听接口和有效按键过滤策略**

最小接口建议：

```csharp
public interface IGlobalKeyboardListener : IDisposable
{
    event EventHandler<DateTimeOffset>? KeystrokeCaptured;
    void Start();
    void Stop();
}
```

- [ ] **Step 2: 实现 Win32 低级键盘钩子封装**

使用：

- `SetWindowsHookEx`
- `CallNextHookEx`
- `UnhookWindowsHookEx`

过滤规则：

- 单独的修饰键不计数
- 只在 `WM_KEYDOWN` / `WM_SYSKEYDOWN` 记一次

- [ ] **Step 3: 在桌面层完成启动和释放接线**

`AppBootstrapper` 负责：

- 应用启动时启动监听
- 主窗口关闭时停止监听
- 把按键事件转给 `TypingSpeedCalculator`

- [ ] **Step 4: 手工验证全局监听**

Run:

```powershell
dotnet run --project src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
```

Expected: 切到记事本等其他应用输入时，程序内速度数字仍会变化。

- [ ] **Step 5: 提交输入监听**

Run:

```powershell
git add src/KeyboardSpeed.Input.Windows src/KeyboardSpeed.Desktop
git commit -m "feat(输入监听): 增加系统全局键盘测速输入采集"
```

## Task 6: 接入 Windows BLE 扫描、连接和 EMS 发包

**Files:**
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Protocol/EmsPacketChecksum.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Protocol/EmsBleProtocolAdapter.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/IWindowsBlePlatformBridge.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/WindowsBlePlatformBridge.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/BluetoothTelemetryStore.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/BluetoothNotificationParser.cs`
- Create: `src/KeyboardSpeed.Bluetooth.Windows/Runtime/BleDeviceManager.cs`
- Test: `tests/KeyboardSpeed.Tests/Bluetooth/EmsBleProtocolAdapterTests.cs`

- [ ] **Step 1: 从参考项目提取协议测试，先写失败用例**

```csharp
[Fact]
public void Adapter_ShouldCreateStopPacket()
{
    var adapter = new EmsBleProtocolAdapter();
    var bytes = adapter.CreateStopPacket();
    Assert.NotEmpty(bytes);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter FullyQualifiedName~EmsBleProtocolAdapterTests
```

Expected: FAIL

- [ ] **Step 3: 参考 `STS2-Link-YOKONEX` 实现协议适配器**

优先参考：

- `D:/STS2-Link-YOKONEX/src/STS2Bridge/Bluetooth/Protocol/EmsBleProtocolAdapter.cs`
- `D:/STS2-Link-YOKONEX/src/STS2Bridge/Bluetooth/Runtime/WindowsBlePlatformBridge.cs`
- `D:/STS2-Link-YOKONEX/src/STS2Bridge/Bluetooth/Runtime/WindowsBleDeviceManager.cs`

需要保留的最小能力：

- 扫描同类 EMS 设备
- 连接 / 断开
- 发包
- 停止包
- 电量与基础状态更新

- [ ] **Step 4: 运行协议测试确认通过**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj --filter FullyQualifiedName~EmsBleProtocolAdapterTests
```

Expected: PASS

- [ ] **Step 5: 手工验证设备扫描与连接**

Run:

```powershell
dotnet run --project src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
```

Expected:

- 设备页可扫描到目标 EMS 设备
- 点击连接后状态变为已连接
- 可执行停止命令

- [ ] **Step 6: 提交蓝牙接入**

Run:

```powershell
git add src/KeyboardSpeed.Bluetooth.Windows tests/KeyboardSpeed.Tests/Bluetooth
git commit -m "feat(蓝牙接入): 完成 Windows BLE 设备扫描连接与 EMS 发包"
```

## Task 7: 完成现代化桌面壳与概览页

**Files:**
- Create: `src/KeyboardSpeed.Desktop/App.xaml`
- Create: `src/KeyboardSpeed.Desktop/MainWindow.xaml`
- Create: `src/KeyboardSpeed.Desktop/Resources/Colors.xaml`
- Create: `src/KeyboardSpeed.Desktop/Resources/Typography.xaml`
- Create: `src/KeyboardSpeed.Desktop/Resources/Controls.xaml`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/ObservableObject.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/RelayCommand.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/MainViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/ViewModels/OverviewViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/Views/OverviewView.xaml`
- Create: `src/KeyboardSpeed.Desktop/Controls/TrendChartControl.cs`

- [ ] **Step 1: 定义主窗口布局与资源系统**

主窗口至少包含：

- 左侧导航或顶部导航
- 主内容区域
- 顶部状态卡片区

资源层至少包含：

- 基础颜色
- 字体大小体系
- 卡片和按钮样式

- [ ] **Step 2: 实现概览页 ViewModel**

概览页显示：

- 当前连接状态
- 实时 KPM/WPM
- 当前速度区间
- 当前波形名称
- 最近 30 秒趋势数据

- [ ] **Step 3: 实现趋势图控件**

控件能力：

- 接收一组数值点
- 自绘平滑折线或折线段
- 支持空数据与缩放

- [ ] **Step 4: 手工验证主界面视觉和动态数据**

Run:

```powershell
dotnet run --project src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
```

Expected:

- 界面为现代卡片式布局
- 指标数字可实时刷新
- 趋势图有持续更新

- [ ] **Step 5: 提交桌面壳**

Run:

```powershell
git add src/KeyboardSpeed.Desktop
git commit -m "feat(桌面界面): 增加现代化主窗口与概览页"
```

## Task 8: 完成设备页与蓝牙控制交互

**Files:**
- Create: `src/KeyboardSpeed.Desktop/ViewModels/DevicesViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/Views/DevicesView.xaml`
- Modify: `src/KeyboardSpeed.Desktop/ViewModels/MainViewModel.cs`

- [ ] **Step 1: 建立设备列表模型和命令**

设备页最少命令：

- 扫描
- 连接
- 断开
- 刷新状态
- 停止波形

- [ ] **Step 2: 把设备管理器状态映射到 UI**

显示字段至少包括：

- 设备名
- 设备类型
- 连接状态
- 电量
- 最近错误信息

- [ ] **Step 3: 手工验证设备页交互**

Run:

```powershell
dotnet run --project src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
```

Expected:

- 设备列表可刷新
- 连接状态会同步到按钮可用性
- 断开后状态归零

- [ ] **Step 4: 提交设备页**

Run:

```powershell
git add src/KeyboardSpeed.Desktop
git commit -m "feat(设备控制): 增加蓝牙设备管理页面与操作交互"
```

## Task 9: 完成波形库、步骤编辑器和波形预览

**Files:**
- Create: `src/KeyboardSpeed.Desktop/ViewModels/WaveformsViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/Views/WaveformsView.xaml`
- Create: `src/KeyboardSpeed.Desktop/Controls/WaveformPreviewControl.cs`
- Modify: `src/KeyboardSpeed.Core/Waveforms/BuiltinWaveforms.cs`

- [ ] **Step 1: 先写波形预览和波形编辑的失败测试（如有纯逻辑）**

建议给波形预览点集生成逻辑抽成可测试方法：

```csharp
[Fact]
public void PreviewPoints_ShouldRespectStepDuration()
{
    var points = WaveformPreviewBuilder.Build(waveform);
    Assert.True(points.Count > 2);
}
```

- [ ] **Step 2: 实现波形管理命令**

最少能力：

- 新增波形
- 复制波形
- 删除波形
- 新增步骤
- 删除步骤
- 调整步骤顺序

- [ ] **Step 3: 实现波形预览控件与试播按钮**

试播流程：

- 读取当前编辑波形
- 发送到 `BleDeviceManager`
- 提供停止按钮

- [ ] **Step 4: 手工验证波形编辑和试播**

Run:

```powershell
dotnet run --project src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
```

Expected:

- 可新增和编辑波形步骤
- 预览图会随参数变化
- 点击试播可下发设备

- [ ] **Step 5: 提交波形库**

Run:

```powershell
git add src/KeyboardSpeed.Core/Waveforms src/KeyboardSpeed.Desktop tests/KeyboardSpeed.Tests
git commit -m "feat(波形编辑): 支持波形库管理步骤编辑与图形预览"
```

## Task 10: 完成速度区间事件绑定页

**Files:**
- Create: `src/KeyboardSpeed.Desktop/ViewModels/RulesViewModel.cs`
- Create: `src/KeyboardSpeed.Desktop/Views/RulesView.xaml`
- Modify: `src/KeyboardSpeed.Core/Rules/SpeedRuleCoordinator.cs`
- Modify: `src/KeyboardSpeed.Desktop/ViewModels/MainViewModel.cs`

- [ ] **Step 1: 建立规则列表和编辑表单**

字段至少包含：

- 规则名称
- 指标类型
- 最小值 / 最大值
- 目标波形
- 冷却时间
- 启用开关
- 离开区间停止开关

- [ ] **Step 2: 把规则变更接入运行时协调器**

要求：

- 保存后立即生效
- 当前速度变化时自动应用最新规则

- [ ] **Step 3: 手工验证区间切换**

Run:

```powershell
dotnet run --project src/KeyboardSpeed.Desktop/KeyboardSpeed.Desktop.csproj
```

Expected:

- 输入速度从低区到高区时，当前波形会切换
- 冷却时间内不会反复抖动切换
- 离开区间后按配置停止

- [ ] **Step 4: 提交规则绑定页**

Run:

```powershell
git add src/KeyboardSpeed.Core/Rules src/KeyboardSpeed.Desktop
git commit -m "feat(事件绑定): 支持按速度区间绑定蓝牙波形"
```

## Task 11: 完成配置保存、启动恢复和应用编排

**Files:**
- Modify: `src/KeyboardSpeed.Core/Configuration/SettingsStore.cs`
- Modify: `src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs`
- Modify: `src/KeyboardSpeed.Desktop/ViewModels/MainViewModel.cs`

- [ ] **Step 1: 启动时加载配置并恢复波形和规则**

App 启动流程：

- 解析 `%AppData%\KeyboardSpeed-YOKONEX\app-settings.json`
- 不存在时创建默认配置
- 把配置注入 ViewModel 和运行时服务

- [ ] **Step 2: 配置变更后自动保存**

建议使用：

- 显式保存命令
- 或短延迟防抖自动保存

- [ ] **Step 3: 手工验证配置恢复**

Expected:

- 重启应用后规则仍在
- 波形库仍在
- 最近设备 ID 仍在

- [ ] **Step 4: 提交配置恢复**

Run:

```powershell
git add src/KeyboardSpeed.Core/Configuration src/KeyboardSpeed.Desktop
git commit -m "feat(配置持久化): 支持应用设置与波形规则自动保存恢复"
```

## Task 12: 全量验证与文档补充

**Files:**
- Modify: `docs/superpowers/specs/2026-05-18-keyboard-speed-ble-design.md`
- Create: `README.md`
- Optionally Create: `docs/manual-test-checklist.md`

- [ ] **Step 1: 运行完整测试**

Run:

```powershell
dotnet test tests/KeyboardSpeed.Tests/KeyboardSpeed.Tests.csproj
```

Expected: PASS

- [ ] **Step 2: 运行完整构建**

Run:

```powershell
dotnet build KeyboardSpeed-YOKONEX.sln -c Release
```

Expected: PASS

- [ ] **Step 3: 执行手工联调清单**

至少验证：

- 全局测速
- BLE 扫描连接
- 波形试播
- 区间切换
- 配置恢复

- [ ] **Step 4: 更新 README 与验证文档**

README 至少包含：

- 项目简介
- 构建方式
- 运行方式
- 权限提示
- 配置文件路径

- [ ] **Step 5: 提交收尾文档**

Run:

```powershell
git add README.md docs
git commit -m "docs(使用说明): 补充项目运行方式与联调验证文档"
```

