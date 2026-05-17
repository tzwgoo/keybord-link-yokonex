namespace KeyboardSpeed.Core.Rules;

public static class BuiltinSpeedRulePresets
{
    public static IReadOnlyList<SpeedRulePreset> CreateDefaults()
    {
        return
        [
            new SpeedRulePreset(
                "low-focus",
                "低速专注",
                "适合 0 到 119.99 KPM 的低速专注输入，使用柔和波形保持轻反馈。",
                0d,
                119.99d,
                1800,
                "soft-pulse",
                true,
                true),
            new SpeedRulePreset(
                "mid-rhythm",
                "中速节奏",
                "适合 120 到 220 KPM 的持续输入节奏，推荐使用心跳波形。",
                120d,
                220d,
                1500,
                "heartbeat",
                true,
                true),
            new SpeedRulePreset(
                "high-sprint",
                "高速冲刺",
                "适合 220.01 KPM 以上的冲刺输入，触发更明显的爆发感。",
                220.01d,
                999d,
                900,
                "heartbeat",
                true,
                true)
        ];
    }
}
