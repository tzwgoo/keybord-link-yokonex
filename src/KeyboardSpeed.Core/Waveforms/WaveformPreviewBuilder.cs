namespace KeyboardSpeed.Core.Waveforms;

public static class WaveformPreviewBuilder
{
    public static WaveformPreview Build(EmsWaveformDefinition waveform)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        var points = new List<WaveformPreviewPoint>();
        var currentTime = 0;
        foreach (var step in waveform.Steps)
        {
            var safeDuration = Math.Max(1, step.DurationMs);
            points.Add(new WaveformPreviewPoint(currentTime, step.AStrength, step.BStrength));
            currentTime += safeDuration;
            points.Add(new WaveformPreviewPoint(currentTime, step.AStrength, step.BStrength));
        }

        return new WaveformPreview(points, currentTime);
    }
}
