# Windows Ink 数位板实时压感映射 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为当前桌面程序增加第一版 Windows Ink 数位板支持，在全局笔尖接触期间将压感实时映射为 EMS 输出强度，并在压感不可用时回退为固定波形触发。

**Architecture:** 保持现有键盘测速与规则链路不变，把输入层升级为“通用输入事件”模型，再为 Windows Ink 增加一个独立的笔输入后端。`AppBootstrapper` 根据输入来源分流：键盘继续走测速和规则，数位板走压感映射与实时强度输出；设备层新增实时强度发送能力，与现有预设波形播放并行存在。

**Tech Stack:** C#、.NET 9、WPF、Windows Ink / 原生 Windows 输入链路、Windows BLE、xUnit

---

## File Structure

**Create**

- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\InputSourceType.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\InputPhase.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\GlobalInputEventArgs.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\IGlobalInputListener.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\WindowsInkPenInputListener.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Input\PenPressureMappingOptions.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Input\PenPressureMapper.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Input\PenRealtimeOutputState.cs`
- `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Input\PenPressureMapperTests.cs`
- `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Input\WindowsInkPenInputListenerTests.cs`

**Modify**

- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\IGlobalKeyboardListener.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\GlobalKeyboardListener.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\GlobalKeyboardHook.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\KeystrokeCapturedEventArgs.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\KeyboardSpeed.Input.Windows.csproj`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Configuration\AppSettings.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Bluetooth.Windows\Runtime\BleDeviceManager.cs`
- `D:\keybord-link-yokonex\src\KeyboardSpeed.Desktop\Services\AppBootstrapper.cs`
- `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Bluetooth\BleDeviceManagerTests.cs`
- `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Configuration\SettingsStoreTests.cs`
- `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Desktop\AppBootstrapperTriggerModeTests.cs`

**Notes**

- 第一版不加 UI 配置入口，只增加默认参数与可持久化设置字段，允许后续直接扩展设置页面。
- 回退固定波形优先复用 `AppSettings.KeypressWaveformId`，避免现在就引入一整套新的波形选择 UI。

### Task 1: 升级输入模型为通用输入事件

**Files:**
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\InputSourceType.cs`
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\InputPhase.cs`
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\GlobalInputEventArgs.cs`
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\IGlobalInputListener.cs`
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\IGlobalKeyboardListener.cs`
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\GlobalKeyboardListener.cs`
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\KeystrokeCapturedEventArgs.cs`
- Test: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Input\KeyboardInputClassifierTests.cs`
- Test: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Desktop\AppBootstrapperTriggerModeTests.cs`

- [ ] **Step 1: 写失败测试，约束键盘事件能投影到通用输入事件**

```csharp
[Fact]
public void KeyboardEvent_ShouldExposeKeyboardSourceAndDownPhase()
{
    var input = GlobalInputEventArgs.FromKeyboard(
        new KeystrokeCapturedEventArgs(DateTimeOffset.Parse("2026-05-19T12:00:00+08:00"), 65));

    Assert.Equal(InputSourceType.Keyboard, input.SourceType);
    Assert.Equal(InputPhase.Down, input.Phase);
    Assert.Equal(65, input.VirtualKey);
    Assert.Null(input.Pressure);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~KeyboardEvent_ShouldExposeKeyboardSourceAndDownPhase"`

Expected: FAIL，提示缺少通用输入模型类型或工厂方法。

- [ ] **Step 3: 增加通用输入模型并让键盘监听器兼容**

```csharp
public enum InputSourceType
{
    Keyboard,
    Pen
}

public enum InputPhase
{
    Down,
    Move,
    Up
}

public sealed class GlobalInputEventArgs : EventArgs
{
    public static GlobalInputEventArgs FromKeyboard(KeystrokeCapturedEventArgs args) => new(...)
}
```

- [ ] **Step 4: 回归键盘现有行为**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~KeyboardInputClassifierTests|FullyQualifiedName~AnyKeypressMode_ShouldDispatchWaveformForEachCapturedKeystroke"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add D:/keybord-link-yokonex/src/KeyboardSpeed.Input.Windows D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Input D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Desktop/AppBootstrapperTriggerModeTests.cs
git commit -m "feat(input): 升级为通用输入事件模型"
```

### Task 2: 实现压感映射与节流核心

**Files:**
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Input\PenPressureMappingOptions.cs`
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Input\PenPressureMapper.cs`
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Input\PenRealtimeOutputState.cs`
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Core\Configuration\AppSettings.cs`
- Test: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Input\PenPressureMapperTests.cs`
- Test: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Configuration\SettingsStoreTests.cs`

- [ ] **Step 1: 先写映射器失败测试**

```csharp
[Theory]
[InlineData(0.00, 0)]
[InlineData(0.05, 0)]
[InlineData(0.08, 18)]
[InlineData(1.00, 62)]
public void Mapper_ShouldApplyDeadZoneAndStrengthBounds(double pressure, int expectedStrength)
{
    var mapper = new PenPressureMapper(new PenPressureMappingOptions());
    Assert.Equal(expectedStrength, mapper.MapStrength(pressure));
}
```

- [ ] **Step 2: 再写节流状态失败测试**

```csharp
[Fact]
public void OutputState_ShouldSkipTinyChangesInsideThrottleWindow()
{
    var state = new PenRealtimeOutputState(TimeSpan.FromMilliseconds(50), minimumStrengthDelta: 2);

    Assert.True(state.ShouldSend(28, DateTimeOffset.Parse("2026-05-19T12:00:00+08:00")));
    Assert.False(state.ShouldSend(29, DateTimeOffset.Parse("2026-05-19T12:00:00.020+08:00")));
}
```

- [ ] **Step 3: 实现最小映射器与默认设置字段**

```csharp
public sealed record PenPressureMappingOptions
{
    public double DeadZone { get; init; } = 0.08;
    public int MinStrength { get; init; } = 18;
    public int MaxStrength { get; init; } = 62;
    public int UpdateIntervalMs { get; init; } = 50;
    public int MinimumStrengthDelta { get; init; } = 2;
}
```

- [ ] **Step 4: 持久化默认设置但不暴露 UI**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~PenPressureMapperTests|FullyQualifiedName~SettingsStoreTests"`

Expected: PASS，说明映射参数默认值与序列化都稳定。

- [ ] **Step 5: Commit**

```bash
git add D:/keybord-link-yokonex/src/KeyboardSpeed.Core D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Input D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Configuration/SettingsStoreTests.cs
git commit -m "feat(core): 增加数位板压感映射核心"
```

### Task 3: 接入 Windows Ink 数位板输入后端

**Files:**
- Create: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\WindowsInkPenInputListener.cs`
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\KeyboardSpeed.Input.Windows.csproj`
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Input.Windows\GlobalKeyboardListener.cs`
- Test: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Input\WindowsInkPenInputListenerTests.cs`

- [ ] **Step 1: 写失败测试，约束笔事件会输出 Pen/Down/Move/Up**

```csharp
[Fact]
public void PenListener_ShouldTranslatePenContactLifecycleIntoInputEvents()
{
    var listener = new WindowsInkPenInputListener(new FakePenSource());
    var events = new List<GlobalInputEventArgs>();
    listener.InputCaptured += (_, args) => events.Add(args);

    listener.TestSource.RaiseDown(pressure: 0.32);
    listener.TestSource.RaiseMove(pressure: 0.61);
    listener.TestSource.RaiseUp();

    Assert.Collection(events,
        first => Assert.Equal(InputPhase.Down, first.Phase),
        second => Assert.Equal(InputPhase.Move, second.Phase),
        third => Assert.Equal(InputPhase.Up, third.Phase));
}
```

- [ ] **Step 2: 先做可测试封装，不直接把系统 API 写死**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~WindowsInkPenInputListenerTests"`

Expected: FAIL，提示监听器或假源接口未实现。

- [ ] **Step 3: 实现 Windows Ink 监听器与可替换输入源**

```csharp
public sealed class WindowsInkPenInputListener : IGlobalInputListener
{
    public event EventHandler<GlobalInputEventArgs>? InputCaptured;
    // 内部桥接真实 Windows Ink 源，测试中注入 Fake 源
}
```

- [ ] **Step 4: 验证数位板接入不破坏输入项目构建**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~WindowsInkPenInputListenerTests|FullyQualifiedName~KeyboardInputClassifierTests"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add D:/keybord-link-yokonex/src/KeyboardSpeed.Input.Windows D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Input
git commit -m "feat(input): 增加 Windows Ink 数位板监听后端"
```

### Task 4: 在设备层增加实时强度输出能力

**Files:**
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Bluetooth.Windows\Runtime\BleDeviceManager.cs`
- Modify: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Bluetooth\BleDeviceManagerTests.cs`

- [ ] **Step 1: 写失败测试，约束实时强度写入与停止行为**

```csharp
[Fact]
public async Task SendRealtimeStrengthAsync_ShouldWriteImmediatePacketAndSkipAutostop()
{
    var manager = CreateConnectedManager(out var device);

    await manager.SendRealtimeStrengthAsync(aStrength: 30, bStrength: 28, CancellationToken.None);

    Assert.Single(manager.PacketHistory);
    Assert.Equal(device.DeviceId, manager.CurrentStatus.Device?.DeviceId);
}
```

- [ ] **Step 2: 写失败测试，约束调用 StopAsync 会终止实时输出**

```csharp
[Fact]
public async Task StopAsync_ShouldStopRealtimeStrengthOutput()
{
    var manager = CreateConnectedManager(out _);
    await manager.SendRealtimeStrengthAsync(36, 34, CancellationToken.None);

    await manager.StopAsync(CancellationToken.None);

    Assert.Equal(2, manager.PacketHistory.Count);
}
```

- [ ] **Step 3: 实现最小设备层 API**

```csharp
public Task SendRealtimeStrengthAsync(int aStrength, int bStrength, CancellationToken cancellationToken = default)
{
    CancelPendingAutoStop();
    var packet = _emsProtocolAdapter.CreateRealtimeStrengthPacket(...);
    return WriteAsync(packet, cancellationToken);
}
```

- [ ] **Step 4: 跑 BLE 回归**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~BleDeviceManagerTests"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add D:/keybord-link-yokonex/src/KeyboardSpeed.Bluetooth.Windows/Runtime/BleDeviceManager.cs D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Bluetooth/BleDeviceManagerTests.cs
git commit -m "feat(bluetooth): 支持实时强度输出通道"
```

### Task 5: 在 AppBootstrapper 中集成数位板实时压感与回退波形

**Files:**
- Modify: `D:\keybord-link-yokonex\src\KeyboardSpeed.Desktop\Services\AppBootstrapper.cs`
- Modify: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Desktop\AppBootstrapperTriggerModeTests.cs`
- Modify: `D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\Configuration\SettingsStoreTests.cs`

- [ ] **Step 1: 写失败测试，约束 PenDown/PenMove/PenUp 的全链路行为**

```csharp
[Fact]
public async Task PenPressureFlow_ShouldStartUpdateAndStopRealtimeOutput()
{
    var input = new FakeGlobalInputListener();
    using var bootstrapper = CreateBootstrapper(input, out var deviceManager);

    input.RaisePenDown(pressure: 0.20);
    input.RaisePenMove(pressure: 0.70);
    input.RaisePenUp();

    Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count >= 3, 1000));
}
```

- [ ] **Step 2: 写失败测试，约束压感不可用时回退为固定波形**

```csharp
[Fact]
public async Task PenPressureFlow_ShouldFallbackToConfiguredWaveformWhenPressureIsUnavailable()
{
    var input = new FakeGlobalInputListener();
    using var bootstrapper = CreateBootstrapper(input, out var deviceManager);

    input.RaisePenDown(pressure: null);

    Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > 0, 1000));
    Assert.Equal("柔和脉冲", bootstrapper.CurrentWaveformName);
}
```

- [ ] **Step 3: 最小实现 AppBootstrapper 分流与回退**

```csharp
private async Task HandleInputCapturedAsync(GlobalInputEventArgs input)
{
    if (input.SourceType == InputSourceType.Keyboard)
    {
        HandleKeyboardInput(input);
        return;
    }

    await HandlePenInputAsync(input);
}
```

- [ ] **Step 4: 跑关键回归并整体构建**

Run: `dotnet test D:\keybord-link-yokonex\tests\KeyboardSpeed.Tests\KeyboardSpeed.Tests.csproj --filter "FullyQualifiedName~AppBootstrapperTriggerModeTests|FullyQualifiedName~SettingsStoreTests|FullyQualifiedName~BleDeviceManagerTests|FullyQualifiedName~PenPressureMapperTests|FullyQualifiedName~WindowsInkPenInputListenerTests"`

Expected: PASS

Run: `dotnet build D:\keybord-link-yokonex\KeyboardSpeed-YOKONEX.slnx`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add D:/keybord-link-yokonex/src/KeyboardSpeed.Desktop/Services/AppBootstrapper.cs D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Desktop/AppBootstrapperTriggerModeTests.cs D:/keybord-link-yokonex/tests/KeyboardSpeed.Tests/Configuration/SettingsStoreTests.cs
git commit -m "feat(desktop): 集成 Windows Ink 数位板实时压感输出"
```
