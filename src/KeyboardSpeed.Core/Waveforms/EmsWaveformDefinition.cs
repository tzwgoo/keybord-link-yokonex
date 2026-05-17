namespace KeyboardSpeed.Core.Waveforms;

public sealed record EmsWaveformDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int LoopCount { get; init; } = 1;

    public List<EmsWaveformStep> Steps { get; init; } = [];
}
