namespace KeyboardSpeed.Core.Waveforms;

public sealed record WaveformScriptTemplate(
    string Id,
    string Name,
    string Description,
    string SuggestedWaveformName,
    string Script);
