namespace KeyboardSpeed.Core.Waveforms;

public static class BuiltinWaveforms
{
    public static IReadOnlyList<EmsWaveformDefinition> CreateDefaults()
    {
        return
        [
            new EmsWaveformDefinition
            {
                Id = "soft-pulse",
                Name = "柔和脉冲",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 160,
                        AStrength = 18,
                        BStrength = 16
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 24,
                        BStrength = 20
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 180,
                        AStrength = 12,
                        BStrength = 10
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "heartbeat",
                Name = "心跳节奏",
                LoopCount = 2,
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 90,
                        AStrength = 34,
                        AMode = 2,
                        BStrength = 30,
                        BMode = 2
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 75,
                        AStrength = 42,
                        AMode = 3,
                        BStrength = 38,
                        BMode = 3,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 180,
                        AStrength = 14,
                        BStrength = 12
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "idle-jolt",
                Name = "高压警醒",
                LoopCount = 2,
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 70,
                        AStrength = 56,
                        AMode = 3,
                        BStrength = 54,
                        BMode = 3,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 60,
                        AStrength = 52,
                        AMode = 2,
                        BStrength = 50,
                        BMode = 2
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 72,
                        AStrength = 60,
                        AMode = 3,
                        BStrength = 58,
                        BMode = 3,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 85,
                        AStrength = 52,
                        AMode = 2,
                        BStrength = 50,
                        BMode = 2
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "alternating-sweep",
                Name = "交替扫动",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 34,
                        AMode = 2,
                        BStrength = 10
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 10,
                        BStrength = 34,
                        BMode = 2
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 30,
                        AMode = 3,
                        BStrength = 16
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 16,
                        BStrength = 30,
                        BMode = 3
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "sprint-burst",
                Name = "冲刺爆发",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 80,
                        AStrength = 34,
                        AMode = 3,
                        BStrength = 34,
                        BMode = 3,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 80,
                        AStrength = 38,
                        AMode = 3,
                        BStrength = 38,
                        BMode = 3,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 120,
                        AStrength = 26,
                        AMode = 2,
                        BStrength = 26,
                        BMode = 2
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "stair-ramp",
                Name = "阶梯爬升",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 110,
                        AStrength = 12,
                        BStrength = 10
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 110,
                        AStrength = 20,
                        BStrength = 18
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 110,
                        AStrength = 28,
                        BStrength = 24
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 36,
                        AMode = 2,
                        BStrength = 30,
                        BMode = 2
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "wave-cascade",
                Name = "波浪级联",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 100,
                        AStrength = 18,
                        BStrength = 28,
                        BMode = 2
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 120,
                        AStrength = 26,
                        AMode = 2,
                        BStrength = 18
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 120,
                        AStrength = 34,
                        AMode = 3,
                        BStrength = 30,
                        BMode = 2
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 140,
                        AStrength = 20,
                        BStrength = 24
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "double-knock",
                Name = "双击敲击",
                LoopCount = 2,
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 90,
                        AStrength = 34,
                        AMode = 3,
                        BStrength = 32,
                        BMode = 3,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 80,
                        AStrength = 12,
                        BStrength = 10
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 95,
                        AStrength = 36,
                        AMode = 3,
                        BStrength = 34,
                        BMode = 3,
                        MotorState = 1
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "center-lock",
                Name = "中心锁定",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 150,
                        AStrength = 28,
                        AMode = 2,
                        BStrength = 28,
                        BMode = 2
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 110,
                        AStrength = 20,
                        BStrength = 20
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 150,
                        AStrength = 30,
                        AMode = 2,
                        BStrength = 30,
                        BMode = 2,
                        MotorState = 1
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "rolling-gallop",
                Name = "滚动疾驰",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 85,
                        AStrength = 24,
                        AMode = 3,
                        BStrength = 14
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 85,
                        AStrength = 14,
                        BStrength = 28,
                        BMode = 3
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 85,
                        AStrength = 30,
                        AMode = 3,
                        BStrength = 18,
                        MotorState = 1
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 115,
                        AStrength = 18,
                        BStrength = 24,
                        BMode = 2
                    }
                ]
            }
        ];
    }
}
