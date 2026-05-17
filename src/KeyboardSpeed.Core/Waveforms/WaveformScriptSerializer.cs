using System.Globalization;
using System.Text;

namespace KeyboardSpeed.Core.Waveforms;

public static class WaveformScriptSerializer
{
    public static List<EmsWaveformStep> Parse(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return [new EmsWaveformStep()];
        }

        var steps = new List<EmsWaveformStep>();
        var lines = script
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 6)
            {
                throw new FormatException($"波形脚本行格式不正确: {line}");
            }

            steps.Add(new EmsWaveformStep
            {
                DurationMs = ParseInt(parts[0], nameof(EmsWaveformStep.DurationMs)),
                AStrength = ParseInt(parts[1], nameof(EmsWaveformStep.AStrength)),
                AMode = ParseInt(parts[2], nameof(EmsWaveformStep.AMode)),
                BStrength = ParseInt(parts[3], nameof(EmsWaveformStep.BStrength)),
                BMode = ParseInt(parts[4], nameof(EmsWaveformStep.BMode)),
                MotorState = ParseInt(parts[5], nameof(EmsWaveformStep.MotorState))
            });
        }

        return steps;
    }

    public static string Serialize(IEnumerable<EmsWaveformStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var builder = new StringBuilder();
        foreach (var step in steps)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendJoin(
                ',',
                step.DurationMs.ToString(CultureInfo.InvariantCulture),
                step.AStrength.ToString(CultureInfo.InvariantCulture),
                step.AMode.ToString(CultureInfo.InvariantCulture),
                step.BStrength.ToString(CultureInfo.InvariantCulture),
                step.BMode.ToString(CultureInfo.InvariantCulture),
                step.MotorState.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static int ParseInt(string value, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new FormatException($"无法解析 {fieldName}: {value}");
        }

        return result;
    }
}
