namespace KeyboardSpeed.Core.Waveforms;

public static class WaveformStepEditorLogic
{
    public static IReadOnlyList<EmsWaveformStep> InsertStepAfter(IEnumerable<EmsWaveformStep> steps, int index)
    {
        var next = NormalizeSteps(steps).ToList();
        var safeIndex = Math.Max(-1, Math.Min(index, next.Count - 1));
        var template = safeIndex >= 0 ? CloneStep(next[safeIndex]) : new EmsWaveformStep();
        next.Insert(safeIndex + 1, template);
        return next;
    }

    public static IReadOnlyList<EmsWaveformStep> MoveStep(IEnumerable<EmsWaveformStep> steps, int index, bool moveUp)
    {
        var next = NormalizeSteps(steps).ToList();
        var targetIndex = moveUp ? index - 1 : index + 1;
        if (index < 0 || index >= next.Count || targetIndex < 0 || targetIndex >= next.Count)
        {
            return next;
        }

        (next[index], next[targetIndex]) = (next[targetIndex], next[index]);
        return next;
    }

    public static IReadOnlyList<EmsWaveformStep> DeleteStep(IEnumerable<EmsWaveformStep> steps, int index)
    {
        var next = NormalizeSteps(steps).ToList();
        if (index < 0 || index >= next.Count)
        {
            return next;
        }

        next.RemoveAt(index);
        if (next.Count == 0)
        {
            next.Add(new EmsWaveformStep());
        }

        return next;
    }

    public static IReadOnlyList<EmsWaveformStep> UpdateStep(IEnumerable<EmsWaveformStep> steps, int index, EmsWaveformStep updatedStep)
    {
        ArgumentNullException.ThrowIfNull(updatedStep);

        var next = NormalizeSteps(steps).ToList();
        if (index < 0 || index >= next.Count)
        {
            return next;
        }

        next[index] = CloneStep(updatedStep);
        return next;
    }

    private static IReadOnlyList<EmsWaveformStep> NormalizeSteps(IEnumerable<EmsWaveformStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var normalized = steps.Select(CloneStep).ToList();
        if (normalized.Count == 0)
        {
            normalized.Add(new EmsWaveformStep());
        }

        return normalized;
    }

    private static EmsWaveformStep CloneStep(EmsWaveformStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return step with { };
    }
}
