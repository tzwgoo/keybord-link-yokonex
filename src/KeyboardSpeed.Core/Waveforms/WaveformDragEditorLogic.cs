namespace KeyboardSpeed.Core.Waveforms;

public enum WaveformDragHandleKind
{
    ChannelA,
    ChannelB,
    Duration
}

public sealed record WaveformDragHandle(
    int StepIndex,
    WaveformDragHandleKind Kind,
    double X,
    double Y,
    int Value);

public static class WaveformDragEditorLogic
{
    public const int MinDurationMs = 20;
    public const double DefaultPadding = 12d;

    public static IReadOnlyList<WaveformDragHandle> BuildHandles(
        IEnumerable<EmsWaveformStep> steps,
        double width,
        double height,
        double padding = DefaultPadding)
    {
        var normalized = NormalizeSteps(steps).ToList();
        var totalDuration = normalized.Sum(static item => Math.Max(MinDurationMs, item.DurationMs));
        var plotWidth = Math.Max(1d, width - padding * 2);
        var handles = new List<WaveformDragHandle>(normalized.Count * 3);

        var currentTime = 0;
        for (var index = 0; index < normalized.Count; index++)
        {
            var step = normalized[index];
            var safeDuration = Math.Max(MinDurationMs, step.DurationMs);
            var startX = padding + plotWidth * currentTime / totalDuration;
            currentTime += safeDuration;
            var endX = padding + plotWidth * currentTime / totalDuration;
            var centerX = startX + (endX - startX) / 2d;

            handles.Add(new WaveformDragHandle(
                index,
                WaveformDragHandleKind.ChannelA,
                centerX,
                StrengthToY(step.AStrength, height, padding),
                step.AStrength));
            handles.Add(new WaveformDragHandle(
                index,
                WaveformDragHandleKind.ChannelB,
                centerX,
                StrengthToY(step.BStrength, height, padding),
                step.BStrength));

            if (index < normalized.Count - 1)
            {
                handles.Add(new WaveformDragHandle(
                    index,
                    WaveformDragHandleKind.Duration,
                    endX,
                    height - padding / 2d,
                    safeDuration));
            }
        }

        return handles;
    }

    public static IReadOnlyList<EmsWaveformStep> UpdateStrength(
        IEnumerable<EmsWaveformStep> steps,
        int stepIndex,
        WaveformDragHandleKind kind,
        double y,
        double height,
        double padding = DefaultPadding)
    {
        var normalized = NormalizeSteps(steps).ToList();
        if (stepIndex < 0 || stepIndex >= normalized.Count)
        {
            return normalized;
        }

        var updatedStrength = YToStrength(y, height, padding);
        var step = normalized[stepIndex];
        normalized[stepIndex] = kind switch
        {
            WaveformDragHandleKind.ChannelA => step with { AStrength = updatedStrength },
            WaveformDragHandleKind.ChannelB => step with { BStrength = updatedStrength },
            _ => step
        };
        return normalized;
    }

    public static IReadOnlyList<EmsWaveformStep> UpdateDurationFromDelta(
        IEnumerable<EmsWaveformStep> steps,
        int stepIndex,
        double deltaX,
        double width,
        double padding = DefaultPadding)
    {
        var normalized = NormalizeSteps(steps).ToList();
        if (stepIndex < 0 || stepIndex >= normalized.Count)
        {
            return normalized;
        }

        var totalDuration = normalized.Sum(static item => Math.Max(MinDurationMs, item.DurationMs));
        var plotWidth = Math.Max(1d, width - padding * 2);
        var msPerPixel = totalDuration / plotWidth;
        var durationDelta = (int)Math.Round(deltaX * msPerPixel, MidpointRounding.AwayFromZero);
        if (durationDelta == 0 && Math.Abs(deltaX) >= 1d)
        {
            durationDelta = Math.Sign(deltaX);
        }

        var step = normalized[stepIndex];
        normalized[stepIndex] = step with
        {
            DurationMs = Math.Max(MinDurationMs, step.DurationMs + durationDelta)
        };
        return normalized;
    }

    private static int YToStrength(double y, double height, double padding)
    {
        var plotHeight = Math.Max(1d, height - padding * 2);
        var normalized = (height - padding - y) / plotHeight;
        return (int)Math.Round(Math.Clamp(normalized, 0d, 1d) * 100d, MidpointRounding.AwayFromZero);
    }

    private static double StrengthToY(int strength, double height, double padding)
    {
        var plotHeight = Math.Max(1d, height - padding * 2);
        return height - padding - plotHeight * Math.Clamp(strength, 0, 100) / 100d;
    }

    private static IReadOnlyList<EmsWaveformStep> NormalizeSteps(IEnumerable<EmsWaveformStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var normalized = steps.Select(static item => item with { }).ToList();
        if (normalized.Count == 0)
        {
            normalized.Add(new EmsWaveformStep());
        }

        return normalized;
    }
}
