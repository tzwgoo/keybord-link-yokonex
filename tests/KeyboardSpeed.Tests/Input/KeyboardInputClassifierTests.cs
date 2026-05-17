using KeyboardSpeed.Input.Windows;

namespace KeyboardSpeed.Tests.Input;

public sealed class KeyboardInputClassifierTests
{
    [Theory]
    [InlineData(0x41)]
    [InlineData(0x5A)]
    [InlineData(0x30)]
    [InlineData(0x39)]
    [InlineData(0x20)]
    [InlineData(0x0D)]
    [InlineData(0x08)]
    [InlineData(0xBA)]
    public void ShouldCountCommonTypingKeys(int virtualKey)
    {
        Assert.True(KeyboardInputClassifier.ShouldCount(virtualKey));
    }

    [Theory]
    [InlineData(0x10)]
    [InlineData(0x11)]
    [InlineData(0x12)]
    [InlineData(0x5B)]
    [InlineData(0x5C)]
    [InlineData(0x09)]
    [InlineData(0x14)]
    public void ShouldIgnoreModifierAndNonTypingKeys(int virtualKey)
    {
        Assert.False(KeyboardInputClassifier.ShouldCount(virtualKey));
    }
}
