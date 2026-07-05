using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Core.Waveforms;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Tests.Desktop;

public sealed class FloatingTelemetryPresenterTests
{
    [Fact]
    public void BuildState_ShouldShowOverlayWhenDeviceConnected()
    {
        var snapshot = new TypingSpeedSnapshot(132.4, 26.48, 98.2, 19.64, 6);
        var status = new BluetoothConnectionStatus
        {
            IsConnected = true,
            Device = new BluetoothDeviceDescriptor
            {
                DeviceId = "device-1",
                Name = "YOKONEX-01"
            },
            ChannelAStrength = 28,
            ChannelBStrength = 34
        };
        var waveform = new EmsWaveformDefinition
        {
            Id = "heartbeat",
            Name = "心跳节奏",
            Steps = [new EmsWaveformStep { DurationMs = 120, AStrength = 24, BStrength = 20 }]
        };

        var state = FloatingTelemetryPresenter.BuildState(
            snapshot,
            status,
            "中速区",
            waveform,
            "心跳节奏");

        Assert.True(state.IsVisible);
        Assert.Equal("YOKONEX-01", state.DeviceName);
        Assert.Equal("132.4", state.CharactersPerMinuteText);
        Assert.Equal("中速区", state.RuleName);
        Assert.Equal("心跳节奏", state.WaveformName);
        Assert.Equal(28, state.ChannelAStrength);
        Assert.Equal(34, state.ChannelBStrength);
        Assert.Same(waveform, state.Waveform);
    }

    [Fact]
    public void BuildState_ShouldHideOverlayWhenDeviceDisconnected()
    {
        var snapshot = new TypingSpeedSnapshot(64, 12.8, 72, 14.4, 3);
        var status = new BluetoothConnectionStatus
        {
            IsConnected = false
        };

        var state = FloatingTelemetryPresenter.BuildState(
            snapshot,
            status,
            "未命中",
            null,
            "未触发");

        Assert.False(state.IsVisible);
        Assert.Equal("未连接设备", state.DeviceName);
        Assert.Equal("--", state.ConnectionText);
    }

    [Fact]
    public void BuildState_ShouldClampRealtimeStrengthToDeviceMax()
    {
        var snapshot = new TypingSpeedSnapshot(90, 18, 88, 17.6, 4);
        var status = new BluetoothConnectionStatus
        {
            IsConnected = true,
            ChannelAStrength = 240,
            ChannelBStrength = 200
        };

        var state = FloatingTelemetryPresenter.BuildState(
            snapshot,
            status,
            "未命中",
            null,
            "未触发");

        Assert.Equal(EmsWaveformStep.MaxStrength, state.ChannelAStrength);
        Assert.Equal(EmsWaveformStep.MaxStrength, state.ChannelBStrength);
    }
}
