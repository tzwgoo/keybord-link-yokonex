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
        var totalCount = CountUntil(now);
        var realtimeCount = CountWithinWindow(now, _options.RealtimeWindow);
        var trendCount = CountWithinWindow(now, _options.TrendWindow);
        var realtimeKpm = ConvertTotalCountToCpm(totalCount, now);
        var trendKpm = ConvertCountToKpm(trendCount, _options.TrendWindow);

        return new TypingSpeedSnapshot(
            realtimeKpm,
            realtimeKpm / 5d,
            trendKpm,
            trendKpm / 5d,
            realtimeCount);
    }

    private int CountUntil(DateTimeOffset now)
    {
        return _samples.Count(sample => sample.Timestamp <= now);
    }

    private int CountWithinWindow(DateTimeOffset now, TimeSpan window)
    {
        var minTimestamp = now - window;
        return _samples.Count(sample => sample.Timestamp >= minTimestamp && sample.Timestamp <= now);
    }

    private double ConvertTotalCountToCpm(int count, DateTimeOffset now)
    {
        if (count <= 0)
        {
            return 0d;
        }

        var firstTimestamp = _samples
            .Where(sample => sample.Timestamp <= now)
            .Min(static sample => sample.Timestamp);
        var elapsed = now - firstTimestamp;

        // CPM 按总输入字符数除以总耗时分钟，耗时为 0 时不放大成异常速度。
        return elapsed > TimeSpan.Zero ? count / elapsed.TotalMinutes : 0d;
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
