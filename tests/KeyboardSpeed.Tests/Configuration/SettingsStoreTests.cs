using KeyboardSpeed.Core.Configuration;
using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Configuration;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directoryPath;

    public SettingsStoreTests()
    {
        _directoryPath = Path.Combine(Path.GetTempPath(), "KeyboardSpeed.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directoryPath);
    }

    [Fact]
    public async Task SettingsStore_ShouldRoundTripRulesAndWaveforms()
    {
        var filePath = Path.Combine(_directoryPath, "settings.json");
        var store = new SettingsStore(filePath);
        var settings = new AppSettings
        {
            TriggerMode = WaveformTriggerMode.AnyKeypress,
            KeypressWaveformId = "soft-pulse",
            IdleTriggerEnabled = true,
            IdleTriggerTimeoutMs = 2400,
            IdleWaveformId = "heartbeat",
            SpeedRules =
            [
                new SpeedRangeRule(
                    "mid",
                    "中速区",
                    SpeedMetricType.Kpm,
                    120,
                    220,
                    "heartbeat",
                    1500,
                    true,
                    true,
                    false,
                    true)
            ],
            Waveforms =
            [
                new EmsWaveformDefinition
                {
                    Id = "heartbeat",
                    Name = "Heartbeat",
                    LoopCount = 2,
                    Steps =
                    [
                        new EmsWaveformStep
                        {
                            DurationMs = 160,
                            AStrength = 42,
                            BStrength = 38
                        }
                    ]
                }
            ]
        };

        await store.SaveAsync(settings, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Single(loaded.SpeedRules);
        Assert.Single(loaded.Waveforms);
        Assert.Equal(WaveformTriggerMode.AnyKeypress, loaded.TriggerMode);
        Assert.Equal("soft-pulse", loaded.KeypressWaveformId);
        Assert.True(loaded.IdleTriggerEnabled);
        Assert.Equal(2400, loaded.IdleTriggerTimeoutMs);
        Assert.Equal("heartbeat", loaded.IdleWaveformId);
        Assert.Equal("mid", loaded.SpeedRules[0].Id);
        Assert.Equal("heartbeat", loaded.Waveforms[0].Id);
        Assert.Equal(42, loaded.Waveforms[0].Steps[0].AStrength);
    }

    [Fact]
    public async Task SettingsStore_Load_ShouldRoundTripRulesAndWaveformsSynchronously()
    {
        var filePath = Path.Combine(_directoryPath, "settings-sync.json");
        var store = new SettingsStore(filePath);
        var settings = new AppSettings
        {
            TriggerMode = WaveformTriggerMode.SpeedRules,
            KeypressWaveformId = "heartbeat",
            IdleTriggerEnabled = true,
            IdleTriggerTimeoutMs = 1800,
            IdleWaveformId = "soft-pulse",
            SpeedRules =
            [
                new SpeedRangeRule(
                    "low",
                    "低速区",
                    SpeedMetricType.Kpm,
                    0,
                    119.99,
                    "soft-pulse",
                    1500,
                    true,
                    true,
                    false,
                    true)
            ],
            Waveforms =
            [
                new EmsWaveformDefinition
                {
                    Id = "soft-pulse",
                    Name = "柔和脉冲",
                    Steps =
                    [
                        new EmsWaveformStep
                        {
                            DurationMs = 160,
                            AStrength = 18,
                            BStrength = 16
                        }
                    ]
                }
            ]
        };

        await store.SaveAsync(settings, CancellationToken.None);
        var loaded = store.Load();

        Assert.Single(loaded.SpeedRules);
        Assert.Single(loaded.Waveforms);
        Assert.Equal(WaveformTriggerMode.SpeedRules, loaded.TriggerMode);
        Assert.Equal("heartbeat", loaded.KeypressWaveformId);
        Assert.True(loaded.IdleTriggerEnabled);
        Assert.Equal(1800, loaded.IdleTriggerTimeoutMs);
        Assert.Equal("soft-pulse", loaded.IdleWaveformId);
        Assert.Equal("low", loaded.SpeedRules[0].Id);
        Assert.Equal("soft-pulse", loaded.Waveforms[0].Id);
        Assert.Equal(18, loaded.Waveforms[0].Steps[0].AStrength);
    }

    [Fact]
    public void AppSettings_Defaults_ShouldBindIdleReminderToIdleJolt()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal("idle-jolt", settings.IdleWaveformId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }
}
