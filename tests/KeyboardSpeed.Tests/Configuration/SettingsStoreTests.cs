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
        Assert.Equal("mid", loaded.SpeedRules[0].Id);
        Assert.Equal("heartbeat", loaded.Waveforms[0].Id);
        Assert.Equal(42, loaded.Waveforms[0].Steps[0].AStrength);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }
}
