namespace KeyboardSpeed.Core.Waveforms;

public sealed record WaveformPreview(IReadOnlyList<WaveformPreviewPoint> Points, int TotalDurationMs);
