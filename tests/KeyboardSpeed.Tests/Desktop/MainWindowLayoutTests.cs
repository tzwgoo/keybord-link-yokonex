namespace KeyboardSpeed.Tests.Desktop;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void TabItemTemplate_ShouldRenderHeaderInsteadOfContent()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("ContentSource=\"Header\"", xaml);
    }

    [Fact]
    public void TabStyle_ShouldUseCompactHeaderSizing()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("<Setter Property=\"MinWidth\" Value=\"72\" />", xaml);
        Assert.Contains("<Setter Property=\"Height\" Value=\"48\" />", xaml);
        Assert.Contains("<Setter Property=\"Padding\" Value=\"18,10\" />", xaml);
    }

    [Fact]
    public void Workspace_ShouldNotContainLongHelperDescriptionsUnderSectionTitles()
    {
        var xaml = ReadMainWindowXaml();

        Assert.DoesNotContain("按主题拆分设备、波形和规则操作，减少在单个长页面来回滚动。", xaml);
        Assert.DoesNotContain("把扫描、连接和基础控制放在左侧，右边专门展示设备反馈，减少状态和操作互相挤压。", xaml);
        Assert.DoesNotContain("左侧只放选择、模板和动作，右侧专门编辑内容，让当前正在调整的波形更聚焦。", xaml);
        Assert.DoesNotContain("把规则选择和预设放在左边，右边只负责编辑当前这条规则，阅读路径会清楚很多。", xaml);
    }

    [Fact]
    public void Workspace_ShouldNotContainOverviewTab()
    {
        var xaml = ReadMainWindowXaml();

        Assert.DoesNotContain("<TabItem Header=\"总览\">", xaml);
        Assert.Contains("<TabItem Header=\"设备\">", xaml);
        Assert.Contains("<TabItem Header=\"波形\">", xaml);
        Assert.Contains("<TabItem Header=\"规则\">", xaml);
    }

    [Fact]
    public void Header_ShouldNotContainLegacyDescriptionCopy()
    {
        var xaml = ReadMainWindowXaml();

        Assert.DoesNotContain("把全局打字速度、蓝牙 EMS 设备控制、波形编辑和规则绑定放进同一个现代化桌面控制台。", xaml);
    }

    [Fact]
    public void Header_ShouldReferenceYokonexLogoAsset()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("Icon=\"Assets/yokonex-logo.png\"", xaml);
        Assert.Contains("Source=\"Assets/yokonex-logo.png\"", xaml);
    }

    [Fact]
    public void ComboBoxDropdown_ShouldDefineReadableItemColors()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("<Style TargetType=\"ComboBoxItem\">", xaml);
        Assert.Contains("<Setter Property=\"Foreground\" Value=\"{StaticResource TextPrimaryBrush}\" />", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#0B1527\" />", xaml);
        Assert.Contains("<Setter Property=\"Foreground\" Value=\"{StaticResource InputTextBrush}\" />", xaml);
        Assert.Contains("<Trigger Property=\"IsHighlighted\" Value=\"True\">", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#17365D\" />", xaml);
        Assert.Contains("<Trigger Property=\"IsSelected\" Value=\"True\">", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#23406A\" />", xaml);
    }

    [Fact]
    public void WaveformWorkspace_ShouldNotContainTemplateUi()
    {
        var xaml = ReadMainWindowXaml();

        Assert.DoesNotContain("脚本模板", xaml);
        Assert.DoesNotContain("WaveformTemplateComboBox", xaml);
        Assert.DoesNotContain("应用模板到编辑器", xaml);
    }

    [Fact]
    public void RuleWorkspace_ShouldNotContainPresetUi()
    {
        var xaml = ReadMainWindowXaml();

        Assert.DoesNotContain("规则预设", xaml);
        Assert.DoesNotContain("RulePresetComboBox", xaml);
        Assert.DoesNotContain("应用预设到表单", xaml);
    }

    [Fact]
    public void RuleWorkspace_ShouldContainTriggerModeSelector()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("Text=\"触发模式\"", xaml);
        Assert.Contains("x:Name=\"TriggerModeComboBox\"", xaml);
        Assert.Contains("Text=\"按键波形\"", xaml);
        Assert.Contains("x:Name=\"KeypressWaveformComboBox\"", xaml);
        Assert.DoesNotContain("Content=\"应用触发模式\"", xaml);
    }

    [Fact]
    public void RuleWorkspace_ShouldContainIdleTriggerSettings()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("Text=\"空闲触发\"", xaml);
        Assert.Contains("x:Name=\"IdleTriggerEnabledCheckBox\"", xaml);
        Assert.Contains("x:Name=\"IdleTriggerTimeoutTextBox\"", xaml);
        Assert.Contains("x:Name=\"IdleWaveformComboBox\"", xaml);
    }

    [Fact]
    public void RuleWorkspace_ShouldContainWaveformPreviewAndPeakStrengthSummary()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("x:Name=\"RuleWaveformPreviewCanvas\"", xaml);
        Assert.Contains("Text=\"波形预览\"", xaml);
        Assert.Contains("x:Name=\"RuleWaveformPeakAText\"", xaml);
        Assert.Contains("x:Name=\"RuleWaveformPeakBText\"", xaml);
        Assert.Contains("Text=\"A 通道最大强度\"", xaml);
        Assert.Contains("Text=\"B 通道最大强度\"", xaml);
    }

    [Fact]
    public void WorkspaceTabs_ShouldWrapLargeTabContentInScrollViewer()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Matches("<TabItem Header=\"设备\">[\\s\\S]*?<ScrollViewer VerticalScrollBarVisibility=\"Auto\">", xaml);
        Assert.Matches("<TabItem Header=\"波形\">[\\s\\S]*?<ScrollViewer VerticalScrollBarVisibility=\"Auto\">", xaml);
        Assert.Matches("<TabItem Header=\"规则\">[\\s\\S]*?<ScrollViewer VerticalScrollBarVisibility=\"Auto\">", xaml);
    }

    [Fact]
    public void SpeedUi_ShouldUseCharactersPerMinuteLabels()
    {
        var xaml = ReadMainWindowXaml();

        Assert.DoesNotContain("Text=\"KPM\"", xaml);
        Assert.DoesNotContain("Text=\"WPM\"", xaml);
        Assert.Contains("Text=\"键/分钟\"", xaml);
        Assert.Contains("Text=\"30 秒趋势\"", xaml);
        Assert.Contains("Text=\"30 秒趋势键/分钟\"", xaml);
        Assert.Contains("Text=\"键/分钟区间\"", xaml);
    }

    [Fact]
    public void WaveformStepStrengthBars_ShouldUseRectangularStyle()
    {
        var codeBehind = ReadMainWindowCodeBehind();

        Assert.Contains("CornerRadius = new CornerRadius(2)", codeBehind);
        Assert.DoesNotContain("Width = Math.Max(8, 1.8 * Math.Clamp(strength, 0, 100)),\r\n            Background = CreateBrush(colorHex),\r\n            CornerRadius = new CornerRadius(999)", codeBehind);
    }

    private static string ReadMainWindowXaml()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(repoRoot, "src", "KeyboardSpeed.Desktop", "MainWindow.xaml");

        return File.ReadAllText(xamlPath);
    }

    private static string ReadMainWindowCodeBehind()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var codeBehindPath = Path.Combine(repoRoot, "src", "KeyboardSpeed.Desktop", "MainWindow.xaml.cs");

        return File.ReadAllText(codeBehindPath);
    }
}
