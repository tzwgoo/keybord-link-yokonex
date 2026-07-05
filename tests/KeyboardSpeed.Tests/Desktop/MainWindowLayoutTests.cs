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
        Assert.DoesNotContain("你的输入节奏正在驱动设备响应", xaml);
        Assert.DoesNotContain("只保留核心状态和当前操作，减少视觉干扰。", xaml);
        Assert.DoesNotContain("主状态固定显示", xaml);
        Assert.DoesNotContain("切换标签时也能看到", xaml);
    }

    [Fact]
    public void Workspace_ShouldNotContainExplanatoryHelperCopy()
    {
        var xaml = ReadMainWindowXaml();
        var codeBehind = ReadMainWindowCodeBehind();

        Assert.DoesNotContain("关键状态固定在左侧，操作切换时不用来回找。", xaml);
        Assert.DoesNotContain("按标签处理设备、波形和规则，右侧保持聚焦。", xaml);
        Assert.DoesNotContain("刷新设备状态后，这里会显示 A/B 通道、电机和步数信息。", xaml);
        Assert.DoesNotContain("修改下列字段后，脚本文本和波形预览会自动同步。", codeBehind);
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

        Assert.Contains("<Style TargetType=\"ComboBox\">", xaml);
        Assert.Contains("<Setter Property=\"Foreground\" Value=\"{StaticResource TextPrimaryBrush}\" />", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#FFFFFF\" />", xaml);
        Assert.Contains("<Setter Property=\"BorderBrush\" Value=\"#D0DDEA\" />", xaml);
        Assert.Contains("<Style TargetType=\"ComboBoxItem\">", xaml);
        Assert.Contains("<Setter Property=\"Foreground\" Value=\"{StaticResource TextPrimaryBrush}\" />", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#FFFFFF\" />", xaml);
        Assert.Contains("<Trigger Property=\"IsHighlighted\" Value=\"True\">", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#EEF6FF\" />", xaml);
        Assert.Contains("<Trigger Property=\"IsSelected\" Value=\"True\">", xaml);
        Assert.Contains("<Setter Property=\"Background\" Value=\"#E2F0FF\" />", xaml);
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
    public void RuleWorkspace_ShouldContainSpecificKeyTriggerSettings()
    {
        var xaml = ReadMainWindowXaml();

        Assert.Contains("Text=\"指定按键映射\"", xaml);
        Assert.Contains("x:Name=\"SpecificKeyKeyboardPanel\"", xaml);
        Assert.Contains("x:Name=\"SpecificKeyTextBox\"", xaml);
        Assert.Contains("x:Name=\"SpecificKeyBindingStatusText\"", xaml);
        Assert.Contains("x:Name=\"SpecificKeyWaveformComboBox\"", xaml);
        Assert.Contains("Content=\"保存映射\"", xaml);
        Assert.Contains("Content=\"删除映射\"", xaml);
        Assert.DoesNotContain("x:Name=\"SpecificKeyBindingsComboBox\"", xaml);
        Assert.DoesNotContain("Text=\"点击键帽选中按键。已保存映射的键会高亮显示。\"", xaml);
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
        Assert.Contains("Text=\"字符/分钟\"", xaml);
        Assert.Contains("Text=\"30 秒趋势\"", xaml);
        Assert.Contains("Text=\"30 秒趋势字符/分钟\"", xaml);
        Assert.Contains("Text=\"字符/分钟区间\"", xaml);
    }

    [Fact]
    public void WaveformStepStrengthBars_ShouldUseRectangularStyle()
    {
        var codeBehind = ReadMainWindowCodeBehind();

        Assert.Contains("CornerRadius = new CornerRadius(2)", codeBehind);
        Assert.DoesNotContain("Width = Math.Max(8, 1.8 * Math.Clamp(strength, 0, 100)),\r\n            Background = CreateBrush(colorHex),\r\n            CornerRadius = new CornerRadius(999)", codeBehind);
    }

    [Fact]
    public void SpecificKeyKeyboard_ShouldContainFunctionAndNavigationKeys()
    {
        var codeBehind = ReadMainWindowCodeBehind();

        Assert.Contains("new KeyboardKeyDefinition(0x70, \"F1\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x7B, \"F12\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x2C, \"Prt\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x91, \"Scr\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x13, \"Pau\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x2D, \"Ins\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x22, \"PgDn\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0xC0, \"`\")", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0xA1, \"Shift\", 2.2)", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0xA5, \"Alt\", 1.3)", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x5C, \"Win\", 1.3)", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0x5D, \"Menu\", 1.3)", codeBehind);
        Assert.Contains("new KeyboardKeyDefinition(0xA3, \"Ctrl\", 1.4)", codeBehind);
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
