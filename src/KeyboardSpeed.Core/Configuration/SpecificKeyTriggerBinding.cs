namespace KeyboardSpeed.Core.Configuration;

public sealed record SpecificKeyTriggerBinding
{
    public int VirtualKey { get; init; }

    public string WaveformId { get; init; } = string.Empty;
}
