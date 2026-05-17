using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class WaveformScriptSerializerTests
{
    [Fact]
    public void Parse_ShouldReadMultipleSteps()
    {
        const string script = """
100,10,1,20,2,0
180,30,3,40,4,1
""";

        var steps = WaveformScriptSerializer.Parse(script);

        Assert.Equal(2, steps.Count);
        Assert.Equal(100, steps[0].DurationMs);
        Assert.Equal(20, steps[0].BStrength);
        Assert.Equal(180, steps[1].DurationMs);
        Assert.Equal(1, steps[1].MotorState);
    }

    [Fact]
    public void Serialize_ShouldRoundTripSteps()
    {
        var steps = new[]
        {
            new EmsWaveformStep
            {
                DurationMs = 120,
                AStrength = 12,
                AMode = 1,
                BStrength = 24,
                BMode = 2,
                MotorState = 1
            }
        };

        var script = WaveformScriptSerializer.Serialize(steps);

        Assert.Equal("120,12,1,24,2,1", script.Trim());
    }
}
