namespace KeyboardSpeed.Core.Waveforms;

public static class WaveformPreviewBuilder
{
    public static WaveformPreview Build(EmsWaveformDefinition waveform)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        var points = new List<WaveformPreviewPoint>();
        var currentTime = 0;
        var loopCount = Math.Max(1, waveform.LoopCount);

        // 预览要和实际播放时长一致：循环波形需要按 LoopCount 展开绘制。
        for (var loopIndex = 0; loopIndex < loopCount; loopIndex++)
        {
            foreach (var step in waveform.Steps)
            {
                var safeDuration = Math.Max(1, step.DurationMs);
                points.Add(new WaveformPreviewPoint(currentTime, step.AStrength, step.BStrength));
                currentTime += safeDuration;
                points.Add(new WaveformPreviewPoint(currentTime, step.AStrength, step.BStrength));
            }
        }

        return new WaveformPreview(points, currentTime);
    }
}
