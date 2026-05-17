namespace KeyboardSpeed.Core.Waveforms;

public static class BuiltinWaveformTemplates
{
    public static IReadOnlyList<WaveformScriptTemplate> CreateDefaults()
    {
        return
        [
            new WaveformScriptTemplate(
                "soft-pulse-template",
                "柔和脉冲",
                "适合低速区间，单步轻推，适合长期联动。",
                "柔和脉冲",
                "180,24,1,20,1,0"),
            new WaveformScriptTemplate(
                "heartbeat-template",
                "心跳节奏",
                "双拍节奏，适合中速输入的稳定反馈。",
                "心跳节奏",
                """
120,36,1,32,1,0
90,18,1,16,1,0
"""),
            new WaveformScriptTemplate(
                "alternating-sweep-template",
                "交替扫动",
                "左右通道错峰推动，适合提示节奏变化。",
                "交替扫动",
                """
140,30,2,12,1,0
140,12,1,30,2,0
140,28,3,18,1,0
"""),
            new WaveformScriptTemplate(
                "sprint-burst-template",
                "冲刺爆发",
                "短促连续三段，适合高速区或冲刺提醒。",
                "冲刺爆发",
                """
80,34,3,34,3,1
80,38,3,38,3,1
120,26,2,26,2,0
""")
        ];
    }
}
