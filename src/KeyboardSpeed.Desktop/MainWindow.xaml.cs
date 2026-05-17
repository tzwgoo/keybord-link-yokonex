using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Core.Waveforms;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Desktop;

public partial class MainWindow : Window
{
    private readonly AppBootstrapper _bootstrapper;
    private bool _isBusy;

    public MainWindow(AppBootstrapper bootstrapper)
    {
        _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
        InitializeComponent();
        WaveformPreviewCanvas.SizeChanged += (_, _) => RenderWaveformPreviewFromEditor();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _bootstrapper.SnapshotUpdated += HandleSnapshotUpdated;
        _bootstrapper.BluetoothStatusUpdated += HandleBluetoothStatusUpdated;
        ApplyListeningState();
        ApplySnapshot(_bootstrapper.CurrentSnapshot);
        ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
        RefreshDeviceList();
        RefreshWaveformTemplateList();
        RefreshWaveformList();
        RefreshRulePresetList();
        RefreshRuleList();
        RefreshOverviewPanels();
        UpdateRuleState();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _bootstrapper.SnapshotUpdated -= HandleSnapshotUpdated;
        _bootstrapper.BluetoothStatusUpdated -= HandleBluetoothStatusUpdated;
    }

    private void HandleSnapshotUpdated(TypingSpeedSnapshot snapshot)
    {
        Dispatcher.Invoke(() => ApplySnapshot(snapshot));
    }

    private void HandleBluetoothStatusUpdated(BluetoothConnectionStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            ApplyBluetoothStatus(status);
            RefreshDeviceList();
            RefreshOverviewPanels();
        });
    }

    private void ApplySnapshot(TypingSpeedSnapshot snapshot)
    {
        KpmText.Text = snapshot.RealtimeKpm.ToString("0.0");
        WpmText.Text = snapshot.RealtimeWpm.ToString("0.0");
        SamplesText.Text = snapshot.ActiveSampleCount.ToString();
        TrendText.Text = snapshot.TrendKpm.ToString("0.0");
        LastKeyText.Text = _bootstrapper.LastKeystrokeAt?.ToLocalTime().ToString("HH:mm:ss") ?? "--:--:--";
        ApplyListeningState();
        RefreshOverviewPanels();
        UpdateRuleState();
    }

    private void ApplyBluetoothStatus(BluetoothConnectionStatus status)
    {
        var connectedDeviceName = status.Device?.Name ?? "未知设备";
        DeviceStatusText.Text = status.IsConnected
            ? $"已连接设备: {connectedDeviceName}"
            : "未连接设备";
        HeaderDeviceBadge.Text = status.IsConnected
            ? $"{connectedDeviceName} 已连接"
            : "设备未连接";
        BatteryText.Text = $"电量: {(status.BatteryLevel.HasValue ? $"{status.BatteryLevel.Value}%" : "--")}";
        ErrorText.Text = $"最近错误: {(string.IsNullOrWhiteSpace(status.LastError) ? "无" : status.LastError)}";
        ConnectButton.IsEnabled = !_isBusy;
        DisconnectButton.IsEnabled = !_isBusy && status.IsConnected;
        StopButton.IsEnabled = !_isBusy && status.IsConnected;
        RefreshButton.IsEnabled = !_isBusy;
        ScanButton.IsEnabled = !_isBusy;
        RefreshOverviewPanels();
    }

    private void RefreshDeviceList()
    {
        var selectedDeviceId = (DevicesComboBox.SelectedItem as DeviceOption)?.DeviceId;
        var options = _bootstrapper.AvailableDevices
            .Select(device => new DeviceOption(
                device.DeviceId,
                $"{device.Name} | {device.DeviceType} | {device.ProtocolProfile}"))
            .ToList();

        DevicesComboBox.ItemsSource = options;
        DevicesComboBox.SelectedItem = options.FirstOrDefault(item => item.DeviceId == selectedDeviceId)
            ?? options.FirstOrDefault();
    }

    private void RefreshWaveformList()
    {
        var selectedWaveformId = (WaveformsComboBox.SelectedItem as EmsWaveformDefinition)?.Id;
        var waveforms = _bootstrapper.Waveforms.ToList();
        WaveformsComboBox.ItemsSource = waveforms;
        RuleWaveformComboBox.ItemsSource = waveforms;
        WaveformsComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == selectedWaveformId)
            ?? waveforms.FirstOrDefault();
        ApplyWaveformEditor(WaveformsComboBox.SelectedItem as EmsWaveformDefinition);
        RefreshOverviewPanels();
    }

    private void RefreshRuleList()
    {
        var selectedRuleId = (RulesComboBox.SelectedItem as SpeedRangeRule)?.Id;
        var rules = _bootstrapper.SpeedRules.ToList();
        RulesComboBox.ItemsSource = rules;
        RulesComboBox.SelectedItem = rules.FirstOrDefault(item => item.Id == selectedRuleId)
            ?? rules.FirstOrDefault();
        ApplyRuleEditor(RulesComboBox.SelectedItem as SpeedRangeRule);
        RefreshOverviewPanels();
    }

    private void RefreshWaveformTemplateList()
    {
        var templates = BuiltinWaveformTemplates.CreateDefaults().ToList();
        WaveformTemplateComboBox.ItemsSource = templates;
        WaveformTemplateComboBox.SelectedItem = templates.FirstOrDefault();
        ApplyWaveformTemplateDescription(WaveformTemplateComboBox.SelectedItem as WaveformScriptTemplate);
    }

    private void RefreshRulePresetList()
    {
        var presets = BuiltinSpeedRulePresets.CreateDefaults().ToList();
        RulePresetComboBox.ItemsSource = presets;
        RulePresetComboBox.SelectedItem = presets.FirstOrDefault();
        ApplyRulePresetDescription(RulePresetComboBox.SelectedItem as SpeedRulePreset);
    }

    private void UpdateRuleState()
    {
        RuleText.Text = $"命中规则: {_bootstrapper.CurrentRuleName}";
        WaveformText.Text = $"当前波形: {_bootstrapper.CurrentWaveformName}";
    }

    private void ApplyListeningState()
    {
        var isListening = _bootstrapper.IsListening;
        StatusText.Text = isListening ? "监听中" : "未监听";
        StatusText.Foreground = CreateBrush(isListening ? "#4ADE80" : "#FBBF24");
        HeaderListeningBadge.Text = isListening ? "监听中" : "监听已停止";
        HeaderListeningBadge.Foreground = CreateBrush(isListening ? "#D1FAE5" : "#FEF3C7");
    }

    private void RefreshOverviewPanels()
    {
        var latestTelemetry = _bootstrapper.BluetoothTelemetry.Samples.LastOrDefault();
        DevicesCountText.Text = _bootstrapper.AvailableDevices.Count.ToString();
        PacketCountText.Text = _bootstrapper.PacketHistoryCount.ToString();
        WaveformsCountText.Text = _bootstrapper.Waveforms.Count.ToString();
        RulesCountText.Text = _bootstrapper.SpeedRules.Count.ToString();
        TelemetryText.Text = latestTelemetry is null
            ? $"遥测样本: {_bootstrapper.BluetoothTelemetry.Samples.Count}"
            : $"遥测样本: {_bootstrapper.BluetoothTelemetry.Samples.Count} | 最近更新 {latestTelemetry.TimestampUtc.ToLocalTime():HH:mm:ss}";
        ConfigPathText.Text = $"配置文件: {_bootstrapper.SettingsFilePath}";
        RefreshTelemetryDetails();
    }

    private void RefreshTelemetryDetails()
    {
        var status = _bootstrapper.BluetoothStatus;
        ChannelAStatusText.Text = $"A 通道: {BuildChannelStatusText(status.ChannelAEnabled, status.ChannelAStrength, status.ChannelAMode, status.ChannelAElectrodeStatus)}";
        ChannelBStatusText.Text = $"B 通道: {BuildChannelStatusText(status.ChannelBEnabled, status.ChannelBStrength, status.ChannelBMode, status.ChannelBElectrodeStatus)}";
        MotorStatusText.Text = $"电机状态: {(status.MotorState.HasValue ? status.MotorState.Value.ToString() : "--")}";
        StepCountStatusText.Text = $"设备步数: {(status.StepCount.HasValue ? status.StepCount.Value.ToString() : "--")}";
        DeviceTelemetryStatusText.Text =
            $"A: {BuildChannelStatusText(status.ChannelAEnabled, status.ChannelAStrength, status.ChannelAMode, status.ChannelAElectrodeStatus)}\n" +
            $"B: {BuildChannelStatusText(status.ChannelBEnabled, status.ChannelBStrength, status.ChannelBMode, status.ChannelBElectrodeStatus)}\n" +
            $"电机: {(status.MotorState.HasValue ? status.MotorState.Value.ToString() : "--")} | 步数: {(status.StepCount.HasValue ? status.StepCount.Value.ToString() : "--")} | 错误码: {(status.ErrorCode.HasValue ? status.ErrorCode.Value.ToString() : "--")}";
    }

    private async void OnScanClicked(object sender, RoutedEventArgs e)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            await _bootstrapper.ScanBluetoothAsync();
            RefreshDeviceList();
        });
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ExecuteBusyActionAsync(() => _bootstrapper.RefreshBluetoothAsync());
    }

    private async void OnConnectClicked(object sender, RoutedEventArgs e)
    {
        if (DevicesComboBox.SelectedItem is not DeviceOption option)
        {
            ErrorText.Text = "最近错误: 请先扫描并选择一个设备。";
            return;
        }

        await ExecuteBusyActionAsync(() => _bootstrapper.ConnectBluetoothAsync(option.DeviceId));
    }

    private async void OnDisconnectClicked(object sender, RoutedEventArgs e)
    {
        await ExecuteBusyActionAsync(() => _bootstrapper.DisconnectBluetoothAsync());
    }

    private async void OnStopClicked(object sender, RoutedEventArgs e)
    {
        await ExecuteBusyActionAsync(() => _bootstrapper.StopWaveformAsync());
    }

    private async void OnPlayWaveformClicked(object sender, RoutedEventArgs e)
    {
        if (WaveformsComboBox.SelectedItem is not EmsWaveformDefinition waveform)
        {
            ErrorText.Text = "最近错误: 请先选择一个波形。";
            return;
        }

        await ExecuteBusyActionAsync(() => _bootstrapper.PlayWaveformAsync(waveform.Id));
        UpdateRuleState();
    }

    private void OnWaveformSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var waveform = WaveformsComboBox.SelectedItem as EmsWaveformDefinition;
        ApplyWaveformEditor(waveform);
    }

    private void OnWaveformTemplateSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ApplyWaveformTemplateDescription(WaveformTemplateComboBox.SelectedItem as WaveformScriptTemplate);
    }

    private void OnRuleSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ApplyRuleEditor(RulesComboBox.SelectedItem as SpeedRangeRule);
    }

    private void OnRulePresetSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ApplyRulePresetDescription(RulePresetComboBox.SelectedItem as SpeedRulePreset);
    }

    private void OnNewWaveformClicked(object sender, RoutedEventArgs e)
    {
        WaveformsComboBox.SelectedItem = null;
        ApplyWaveformEditor(null);
    }

    private void OnApplyWaveformTemplateClicked(object sender, RoutedEventArgs e)
    {
        if (WaveformTemplateComboBox.SelectedItem is not WaveformScriptTemplate template)
        {
            return;
        }

        WaveformNameTextBox.Text = template.SuggestedWaveformName;
        WaveformScriptTextBox.Text = template.Script;
        RenderWaveformPreviewFromEditor();
    }

    private async void OnSaveWaveformClicked(object sender, RoutedEventArgs e)
    {
        var existingId = (WaveformsComboBox.SelectedItem as EmsWaveformDefinition)?.Id;
        await ExecuteBusyActionAsync(() => _bootstrapper.AddOrUpdateWaveformAsync(existingId, WaveformNameTextBox.Text, WaveformScriptTextBox.Text));
        RefreshWaveformList();
        RefreshRuleList();
    }

    private async void OnDeleteWaveformClicked(object sender, RoutedEventArgs e)
    {
        if (WaveformsComboBox.SelectedItem is not EmsWaveformDefinition waveform)
        {
            ErrorText.Text = "最近错误: 请先选择要删除的波形。";
            return;
        }

        await ExecuteBusyActionAsync(() => _bootstrapper.DeleteWaveformAsync(waveform.Id));
        RefreshWaveformList();
        RefreshRuleList();
    }

    private void OnWaveformScriptTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RenderWaveformPreviewFromEditor();
    }

    private void OnNewRuleClicked(object sender, RoutedEventArgs e)
    {
        RulesComboBox.SelectedItem = null;
        ApplyRuleEditor(null);
    }

    private void OnApplyRulePresetClicked(object sender, RoutedEventArgs e)
    {
        if (RulePresetComboBox.SelectedItem is not SpeedRulePreset preset)
        {
            return;
        }

        RuleNameTextBox.Text = preset.Name;
        RuleMinTextBox.Text = preset.MinValue.ToString("0.##");
        RuleMaxTextBox.Text = preset.MaxValue.ToString("0.##");
        RuleCooldownTextBox.Text = preset.CooldownMs.ToString();
        RuleEnabledCheckBox.IsChecked = preset.Enabled;
        RuleStopOnExitCheckBox.IsChecked = preset.StopOnExit;

        if (RuleWaveformComboBox.ItemsSource is IEnumerable<EmsWaveformDefinition> waveforms)
        {
            RuleWaveformComboBox.SelectedItem = waveforms.FirstOrDefault(item => string.Equals(item.Id, preset.WaveformId, StringComparison.OrdinalIgnoreCase))
                ?? waveforms.FirstOrDefault();
        }
    }

    private async void OnSaveRuleClicked(object sender, RoutedEventArgs e)
    {
        if (RuleWaveformComboBox.SelectedItem is not EmsWaveformDefinition waveform)
        {
            ErrorText.Text = "最近错误: 请先为规则选择一个波形。";
            return;
        }

        if (!double.TryParse(RuleMinTextBox.Text, out var minValue) ||
            !double.TryParse(RuleMaxTextBox.Text, out var maxValue) ||
            !int.TryParse(RuleCooldownTextBox.Text, out var cooldownMs))
        {
            ErrorText.Text = "最近错误: 规则区间或冷却时间格式不正确。";
            return;
        }

        var existingId = (RulesComboBox.SelectedItem as SpeedRangeRule)?.Id;
        await ExecuteBusyActionAsync(() => _bootstrapper.AddOrUpdateRuleAsync(
            existingId,
            RuleNameTextBox.Text,
            minValue,
            maxValue,
            waveform.Id,
            cooldownMs,
            RuleEnabledCheckBox.IsChecked == true,
            RuleStopOnExitCheckBox.IsChecked == true));
        RefreshRuleList();
    }

    private async void OnDeleteRuleClicked(object sender, RoutedEventArgs e)
    {
        if (RulesComboBox.SelectedItem is not SpeedRangeRule rule)
        {
            ErrorText.Text = "最近错误: 请先选择要删除的规则。";
            return;
        }

        await ExecuteBusyActionAsync(() => _bootstrapper.DeleteRuleAsync(rule.Id));
        RefreshRuleList();
    }

    private async Task ExecuteBusyActionAsync(Func<Task> action)
    {
        if (_isBusy)
        {
            return;
        }

        try
        {
            _isBusy = true;
            ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
            await action();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"最近错误: {ex.Message}";
        }
        finally
        {
            _isBusy = false;
            ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
        }
    }

    private async Task ExecuteBusyActionAsync(Func<Task<bool>> action)
    {
        await ExecuteBusyActionAsync(async () => _ = await action());
    }

    private void RenderWaveformPreview(EmsWaveformDefinition? waveform)
    {
        WaveformPreviewCanvas.Children.Clear();
        if (waveform is null)
        {
            AddWaveformPlaceholder("请选择一个波形来查看预览");
            return;
        }

        var preview = WaveformPreviewBuilder.Build(waveform);
        if (preview.Points.Count == 0 || preview.TotalDurationMs <= 0)
        {
            AddWaveformPlaceholder("当前波形没有可绘制的数据点");
            return;
        }

        var width = Math.Max(1d, WaveformPreviewCanvas.ActualWidth > 0 ? WaveformPreviewCanvas.ActualWidth : 520d);
        var height = WaveformPreviewCanvas.Height;
        DrawWaveformGuides(width, height);

        var aLine = new Polyline
        {
            Stroke = CreateBrush("#4FD1C5"),
            StrokeThickness = 2
        };
        var bLine = new Polyline
        {
            Stroke = CreateBrush("#F59E0B"),
            StrokeThickness = 2
        };

        const double padding = 8d;
        foreach (var point in preview.Points)
        {
            var x = padding + (width - padding * 2) * point.TimeMs / preview.TotalDurationMs;
            var aY = height - padding - (height - padding * 2) * Math.Clamp(point.AStrength, 0, 100) / 100d;
            var bY = height - padding - (height - padding * 2) * Math.Clamp(point.BStrength, 0, 100) / 100d;
            aLine.Points.Add(new Point(x, aY));
            bLine.Points.Add(new Point(x, bY));
        }

        WaveformPreviewCanvas.Children.Add(aLine);
        WaveformPreviewCanvas.Children.Add(bLine);

        WaveformPreviewCanvas.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "A 通道",
            Foreground = CreateBrush("#4FD1C5"),
            FontSize = 11
        });

        var bLabel = new System.Windows.Controls.TextBlock
        {
            Text = "B 通道",
            Foreground = CreateBrush("#F59E0B"),
            FontSize = 11
        };
        System.Windows.Controls.Canvas.SetLeft(bLabel, Math.Max(80d, width - 56d));
        WaveformPreviewCanvas.Children.Add(bLabel);
    }

    private void RenderWaveformPreviewFromEditor()
    {
        try
        {
            var steps = WaveformScriptSerializer.Parse(WaveformScriptTextBox.Text);
            RenderWaveformPreview(new EmsWaveformDefinition
            {
                Id = "preview",
                Name = WaveformNameTextBox.Text,
                Steps = steps
            });
        }
        catch (FormatException)
        {
            WaveformPreviewCanvas.Children.Clear();
            AddWaveformPlaceholder("脚本格式无效，无法生成预览");
        }
    }

    private void ApplyWaveformEditor(EmsWaveformDefinition? waveform)
    {
        WaveformNameTextBox.Text = waveform?.Name ?? "新波形";
        WaveformScriptTextBox.Text = waveform is null
            ? "120,10,1,10,1,0"
            : WaveformScriptSerializer.Serialize(waveform.Steps);
        RenderWaveformPreviewFromEditor();
    }

    private void ApplyRuleEditor(SpeedRangeRule? rule)
    {
        RuleNameTextBox.Text = rule?.Name ?? "新规则";
        RuleMinTextBox.Text = rule?.MinValue.ToString("0.##") ?? "0";
        RuleMaxTextBox.Text = rule?.MaxValue.ToString("0.##") ?? "120";
        RuleCooldownTextBox.Text = rule?.CooldownMs.ToString() ?? "1500";
        RuleEnabledCheckBox.IsChecked = rule?.Enabled ?? true;
        RuleStopOnExitCheckBox.IsChecked = rule?.StopOnExit ?? true;

        if (RuleWaveformComboBox.ItemsSource is IEnumerable<EmsWaveformDefinition> waveforms)
        {
            RuleWaveformComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == rule?.WaveformId)
                ?? waveforms.FirstOrDefault();
        }
    }

    private void ApplyWaveformTemplateDescription(WaveformScriptTemplate? template)
    {
        WaveformTemplateDescriptionText.Text = template?.Description ?? "模板会自动填入推荐波形名和脚本。";
    }

    private void ApplyRulePresetDescription(SpeedRulePreset? preset)
    {
        RulePresetDescriptionText.Text = preset?.Description ?? "预设会填充推荐速度区间、冷却时间和目标波形。";
    }

    private void DrawWaveformGuides(double width, double height)
    {
        const double padding = 8d;
        for (var index = 0; index < 4; index++)
        {
            var y = padding + (height - padding * 2) * index / 3d;
            WaveformPreviewCanvas.Children.Add(new Line
            {
                X1 = padding,
                X2 = width - padding,
                Y1 = y,
                Y2 = y,
                Stroke = CreateBrush("#1E2D48"),
                StrokeThickness = 1
            });
        }
    }

    private void AddWaveformPlaceholder(string message)
    {
        WaveformPreviewCanvas.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = message,
            Foreground = CreateBrush("#8FA4C6"),
            FontSize = 12
        });
    }

    private static Brush CreateBrush(string hexColor)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor)!;
    }

    private static string BuildChannelStatusText(bool? enabled, int? strength, int? mode, int? electrodeStatus)
    {
        var enabledText = enabled switch
        {
            true => "启用",
            false => "关闭",
            null => "未知"
        };

        return $"{enabledText} | 强度 {(strength.HasValue ? strength.Value.ToString() : "--")} | 模式 {(mode.HasValue ? mode.Value.ToString() : "--")} | 贴片 {(electrodeStatus.HasValue ? electrodeStatus.Value.ToString() : "--")}";
    }

    private sealed record DeviceOption(string DeviceId, string Summary);
}
