namespace KeyboardSpeed.Core.Typing;

public sealed class TypingSpeedCalculator
{
    private readonly TypingSpeedOptions _options;
    private readonly List<KeystrokeSample> _samples = [];

    public TypingSpeedCalculator(TypingSpeedOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void RecordKeystroke(DateTimeOffset timestamp)
    {
        _samples.Add(new KeystrokeSample(timestamp));
    }

    public TypingSpeedSnapshot CreateSnapshot(DateTimeOffset now)
    {
        PruneExpiredSamples(now);

        var realtimeCount = CountWithinWindow(now, _options.RealtimeWindow);
        var trendCount = CountWithinWindow(now, _options.TrendWindow);
        var realtimeKpm = ConvertCountToKpm(realtimeCount, _options.RealtimeWindow);
        var trendKpm = ConvertCountToKpm(trendCount, _options.TrendWindow);

        return new TypingSpeedSnapshot(
            realtimeKpm,
            realtimeKpm / 5d,
            trendKpm,
            trendKpm / 5d,
            realtimeCount);
    }

    private void PruneExpiredSamples(DateTimeOffset now)
    {
        var minTimestamp = now - _options.TrendWindow;
        _samples.RemoveAll(sample => sample.Timestamp < minTimestamp);
    }

    private int CountWithinWindow(DateTimeOffset now, TimeSpan window)
    {
        var minTimestamp = now - window;
        return _samples.Count(sample => sample.Timestamp >= minTimestamp && sample.Timestamp <= now);
    }

    private static double ConvertCountToKpm(int count, TimeSpan window)
    {
        if (count <= 0 || window <= TimeSpan.Zero)
        {
            return 0d;
        }

        return count * 60d / window.TotalSeconds;
    }
}
