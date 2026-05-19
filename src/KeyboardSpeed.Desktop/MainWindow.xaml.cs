using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Diagnostics;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Core.Waveforms;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Desktop;

public partial class MainWindow : Window
{
    private const double WaveformPreviewPadding = WaveformDragEditorLogic.DefaultPadding;
    private static readonly IReadOnlyList<TriggerModeOption> TriggerModeOptions =
    [
        new TriggerModeOption(WaveformTriggerMode.SpeedRules, "键速触发"),
        new TriggerModeOption(WaveformTriggerMode.AnyKeypress, "按键即触发")
    ];
    private readonly AppBootstrapper _bootstrapper;
    private bool _isBusy;
    private bool _isUpdatingWaveformEditor;
    private bool _isUpdatingTriggerModeEditor;
    private IReadOnlyList<WaveformDragHandle> _waveformDragHandles = Array.Empty<WaveformDragHandle>();
    private WaveformDragSession? _activeWaveformDrag;

    public MainWindow(AppBootstrapper bootstrapper)
    {
        _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
        InitializeComponent();
        WaveformPreviewCanvas.SizeChanged += (_, _) => RenderWaveformPreviewFromEditor();
        RuleWaveformPreviewCanvas.SizeChanged += (_, _) => RefreshRuleWaveformPreview();
        WaveformPreviewCanvas.MouseLeftButtonDown += OnWaveformPreviewMouseLeftButtonDown;
        WaveformPreviewCanvas.MouseMove += OnWaveformPreviewMouseMove;
        WaveformPreviewCanvas.MouseLeftButtonUp += OnWaveformPreviewMouseLeftButtonUp;
        WaveformPreviewCanvas.LostMouseCapture += OnWaveformPreviewLostMouseCapture;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _bootstrapper.SnapshotUpdated += HandleSnapshotUpdated;
        _bootstrapper.BluetoothStatusUpdated += HandleBluetoothStatusUpdated;
        TriggerModeComboBox.ItemsSource = TriggerModeOptions;
        ApplyListeningState();
        ApplySnapshot(_bootstrapper.CurrentSnapshot);
        ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
        RefreshDeviceList();
        RefreshWaveformList();
        RefreshRuleList();
        ApplyTriggerModeEditor();
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
        CpmText.Text = snapshot.RealtimeKpm.ToString("0.0");
        TrendHeroText.Text = snapshot.TrendKpm.ToString("0.0");
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
        KeypressWaveformComboBox.ItemsSource = waveforms;
        IdleWaveformComboBox.ItemsSource = waveforms;
        WaveformsComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == selectedWaveformId)
            ?? waveforms.FirstOrDefault();
        KeypressWaveformComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == _bootstrapper.KeypressWaveformId)
            ?? waveforms.FirstOrDefault();
        IdleWaveformComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == _bootstrapper.IdleWaveformId)
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

        AppDiagnostics.WriteInfo("MainWindow.OnConnectClicked", $"用户请求连接设备: {option.DeviceId}");
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

    private void OnRuleSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ApplyRuleEditor(RulesComboBox.SelectedItem as SpeedRangeRule);
    }

    private void OnRuleWaveformSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshRuleWaveformPreview();
    }

    private void OnTriggerModeSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateTriggerModeUi();
        PersistTriggerModeSelectionIfNeeded();
    }

    private void OnKeypressWaveformSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        PersistTriggerModeSelectionIfNeeded();
    }

    private void OnIdleTriggerToggleChanged(object sender, RoutedEventArgs e)
    {
        PersistTriggerModeSelectionIfNeeded();
    }

    private void OnIdleWaveformSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        PersistTriggerModeSelectionIfNeeded();
    }

    private void OnIdleTriggerTimeoutLostFocus(object sender, RoutedEventArgs e)
    {
        PersistTriggerModeSelectionIfNeeded();
    }

    private void OnIdleTriggerTimeoutKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        PersistTriggerModeSelectionIfNeeded();
    }

    private void OnNewWaveformClicked(object sender, RoutedEventArgs e)
    {
        WaveformsComboBox.SelectedItem = null;
        ApplyWaveformEditor(null);
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
        if (_isUpdatingWaveformEditor)
        {
            return;
        }

        RenderWaveformPreviewFromEditor();
        RefreshWaveformStepEditor();
    }

    private void OnAddWaveformStepClicked(object sender, RoutedEventArgs e)
    {
        if (!TryParseWaveformEditorSteps(out var steps))
        {
            ErrorText.Text = "最近错误: 当前脚本格式无效，无法添加步骤。";
            return;
        }

        UpdateWaveformScriptFromSteps(WaveformStepEditorLogic.InsertStepAfter(steps, steps.Count - 1));
    }

    private void OnNewRuleClicked(object sender, RoutedEventArgs e)
    {
        RulesComboBox.SelectedItem = null;
        ApplyRuleEditor(null);
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

    private void PersistTriggerModeSelectionIfNeeded()
    {
        if (_isUpdatingTriggerModeEditor)
        {
            return;
        }

        var selectedMode = GetSelectedTriggerMode();
        var keypressWaveformId = (KeypressWaveformComboBox.SelectedItem as EmsWaveformDefinition)?.Id;
        var idleWaveformId = (IdleWaveformComboBox.SelectedItem as EmsWaveformDefinition)?.Id;
        if (!int.TryParse(IdleTriggerTimeoutTextBox.Text, out var idleTimeoutMs) || idleTimeoutMs <= 0)
        {
            ErrorText.Text = "最近错误: 空闲超时时间必须是大于 0 的整数毫秒值。";
            return;
        }

        _ = PersistTriggerModeSelectionAsync(
            selectedMode,
            keypressWaveformId,
            IdleTriggerEnabledCheckBox.IsChecked == true,
            idleTimeoutMs,
            idleWaveformId);
    }

    private async Task PersistTriggerModeSelectionAsync(
        WaveformTriggerMode selectedMode,
        string? keypressWaveformId,
        bool idleTriggerEnabled,
        int idleTriggerTimeoutMs,
        string? idleWaveformId)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            await _bootstrapper.UpdateTriggerModeAsync(selectedMode, keypressWaveformId);
            await _bootstrapper.UpdateIdleTriggerAsync(idleTriggerEnabled, idleTriggerTimeoutMs, idleWaveformId);
        });
        ApplyTriggerModeEditor();
        UpdateRuleState();
    }

    private async Task ExecuteBusyActionAsync(Func<Task> action)
    {
        if (_isBusy)
        {
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "忽略重复操作：当前仍处于忙碌状态。");
            return;
        }

        try
        {
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "开始执行忙碌操作。");
            _isBusy = true;
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "忙碌状态已设置，准备刷新连接状态 UI。");
            ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "连接状态 UI 刷新完成，准备执行操作委托。");
            await action();
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "操作委托执行完成。");
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("MainWindow.ExecuteBusyActionAsync", ex);
            ErrorText.Text = $"最近错误: {ex.Message}";
        }
        finally
        {
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "进入收尾阶段，准备恢复空闲状态。");
            _isBusy = false;
            ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync", "空闲状态恢复完成。");
        }
    }

    private async Task ExecuteBusyActionAsync(Func<Task<bool>> action)
    {
        AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync<bool>", "开始执行布尔忙碌操作包装器。");
        await ExecuteBusyActionAsync(async () =>
        {
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync<bool>", "准备调用布尔操作委托。");
            var result = await action();
            AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync<bool>", $"布尔操作委托执行完成: result={result}");
        });
        AppDiagnostics.WriteInfo("MainWindow.ExecuteBusyActionAsync<bool>", "布尔忙碌操作包装器执行完成。");
    }

    private void RenderWaveformPreview(EmsWaveformDefinition? waveform)
    {
        _waveformDragHandles = Array.Empty<WaveformDragHandle>();
        WaveformPreviewCanvas.Children.Clear();
        if (waveform is null)
        {
            AddWaveformPlaceholder(WaveformPreviewCanvas, "请选择一个波形来查看预览");
            return;
        }

        var preview = WaveformPreviewBuilder.Build(waveform);
        if (preview.Points.Count == 0 || preview.TotalDurationMs <= 0)
        {
            AddWaveformPlaceholder(WaveformPreviewCanvas, "当前波形没有可绘制的数据点");
            return;
        }

        var width = Math.Max(1d, WaveformPreviewCanvas.ActualWidth > 0 ? WaveformPreviewCanvas.ActualWidth : 520d);
        var height = WaveformPreviewCanvas.Height;
        DrawWaveformPreview(WaveformPreviewCanvas, preview, width, height);

        const double channelHandleRadius = 6d;
        const double durationHandleRadius = 7d;
        var padding = WaveformPreviewPadding;
        _waveformDragHandles = WaveformDragEditorLogic.BuildHandles(waveform.Steps, width, height, padding);
        foreach (var handle in _waveformDragHandles)
        {
            switch (handle.Kind)
            {
                case WaveformDragHandleKind.ChannelA:
                case WaveformDragHandleKind.ChannelB:
                {
                    var color = handle.Kind == WaveformDragHandleKind.ChannelA ? "#4FD1C5" : "#F59E0B";
                    var ellipse = new Ellipse
                    {
                        Width = channelHandleRadius * 2,
                        Height = channelHandleRadius * 2,
                        Fill = CreateBrush(color),
                        Stroke = CreateBrush("#E2E8F0"),
                        StrokeThickness = 1.2,
                        Cursor = Cursors.SizeNS
                    };
                    System.Windows.Controls.Canvas.SetLeft(ellipse, handle.X - channelHandleRadius);
                    System.Windows.Controls.Canvas.SetTop(ellipse, handle.Y - channelHandleRadius);
                    WaveformPreviewCanvas.Children.Add(ellipse);
                    break;
                }
                case WaveformDragHandleKind.Duration:
                {
                    var diamond = new Polygon
                    {
                        Fill = CreateBrush("#93C5FD"),
                        Stroke = CreateBrush("#E2E8F0"),
                        StrokeThickness = 1,
                        Cursor = Cursors.SizeWE,
                        Points = new PointCollection
                        {
                            new Point(handle.X, handle.Y - durationHandleRadius),
                            new Point(handle.X + durationHandleRadius, handle.Y),
                            new Point(handle.X, handle.Y + durationHandleRadius),
                            new Point(handle.X - durationHandleRadius, handle.Y)
                        }
                    };
                    WaveformPreviewCanvas.Children.Add(diamond);
                    break;
                }
            }
        }
    }

    private void RefreshRuleWaveformPreview()
    {
        var waveform = RuleWaveformComboBox.SelectedItem as EmsWaveformDefinition;
        RuleWaveformPreviewCanvas.Children.Clear();
        if (waveform is null)
        {
            RuleWaveformPeakAText.Text = "0%";
            RuleWaveformPeakBText.Text = "0%";
            RuleWaveformDurationText.Text = "0 ms";
            AddWaveformPlaceholder(RuleWaveformPreviewCanvas, "选择绑定波形后，在这里查看预览");
            return;
        }

        RuleWaveformPeakAText.Text = $"{waveform.Steps.DefaultIfEmpty().Max(step => step?.AStrength ?? 0)}%";
        RuleWaveformPeakBText.Text = $"{waveform.Steps.DefaultIfEmpty().Max(step => step?.BStrength ?? 0)}%";
        RuleWaveformDurationText.Text = $"{waveform.Steps.Sum(step => Math.Max(1, step.DurationMs))} ms";

        var preview = WaveformPreviewBuilder.Build(waveform);
        if (preview.Points.Count == 0 || preview.TotalDurationMs <= 0)
        {
            AddWaveformPlaceholder(RuleWaveformPreviewCanvas, "当前波形没有可绘制的数据点");
            return;
        }

        var width = Math.Max(1d, RuleWaveformPreviewCanvas.ActualWidth > 0 ? RuleWaveformPreviewCanvas.ActualWidth : 520d);
        var height = RuleWaveformPreviewCanvas.Height;
        DrawWaveformPreview(RuleWaveformPreviewCanvas, preview, width, height);
    }

    private void DrawWaveformPreview(Canvas canvas, WaveformPreview preview, double width, double height)
    {
        DrawWaveformGuides(canvas, width, height);

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

        var padding = WaveformPreviewPadding;
        foreach (var point in preview.Points)
        {
            var x = padding + (width - padding * 2) * point.TimeMs / preview.TotalDurationMs;
            var aY = height - padding - (height - padding * 2) * Math.Clamp(point.AStrength, 0, 100) / 100d;
            var bY = height - padding - (height - padding * 2) * Math.Clamp(point.BStrength, 0, 100) / 100d;
            aLine.Points.Add(new Point(x, aY));
            bLine.Points.Add(new Point(x, bY));
        }

        canvas.Children.Add(aLine);
        canvas.Children.Add(bLine);
        canvas.Children.Add(new System.Windows.Controls.TextBlock
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
        canvas.Children.Add(bLabel);
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
            AddWaveformPlaceholder(WaveformPreviewCanvas, "脚本格式无效，无法生成预览");
        }
    }

    private void ApplyWaveformEditor(EmsWaveformDefinition? waveform)
    {
        SetWaveformEditorValues(
            waveform?.Name ?? "新波形",
            waveform is null
                ? "120,10,1,10,1,0"
                : WaveformScriptSerializer.Serialize(waveform.Steps));
        RenderWaveformPreviewFromEditor();
        RefreshWaveformStepEditor();
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

        RefreshRuleWaveformPreview();
    }

    private void ApplyTriggerModeEditor()
    {
        _isUpdatingTriggerModeEditor = true;
        try
        {
            TriggerModeComboBox.SelectedItem = TriggerModeOptions.FirstOrDefault(item => item.Mode == _bootstrapper.TriggerMode)
                ?? TriggerModeOptions.First();

            if (KeypressWaveformComboBox.ItemsSource is IEnumerable<EmsWaveformDefinition> waveforms)
            {
                KeypressWaveformComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == _bootstrapper.KeypressWaveformId)
                    ?? waveforms.FirstOrDefault();

                IdleWaveformComboBox.SelectedItem = waveforms.FirstOrDefault(item => item.Id == _bootstrapper.IdleWaveformId)
                    ?? waveforms.FirstOrDefault();
            }

            IdleTriggerEnabledCheckBox.IsChecked = _bootstrapper.IdleTriggerEnabled;
            IdleTriggerTimeoutTextBox.Text = _bootstrapper.IdleTriggerTimeoutMs.ToString();
            UpdateTriggerModeUi();
        }
        finally
        {
            _isUpdatingTriggerModeEditor = false;
        }
    }

    private void UpdateTriggerModeUi()
    {
        var isAnyKeypressMode = GetSelectedTriggerMode() == WaveformTriggerMode.AnyKeypress;
        KeypressModePanel.Visibility = isAnyKeypressMode ? Visibility.Visible : Visibility.Collapsed;
        IdleTriggerPanel.Visibility = Visibility.Visible;
        SpeedRuleListPanel.IsEnabled = !isAnyKeypressMode;
        SpeedRuleListPanel.Opacity = isAnyKeypressMode ? 0.45 : 1;
        SpeedRuleEditorPanel.IsEnabled = !isAnyKeypressMode;
        SpeedRuleEditorPanel.Opacity = isAnyKeypressMode ? 0.45 : 1;
    }

    private void DrawWaveformGuides(Canvas canvas, double width, double height)
    {
        var padding = WaveformPreviewPadding;
        for (var index = 0; index < 4; index++)
        {
            var y = padding + (height - padding * 2) * index / 3d;
            canvas.Children.Add(new Line
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

    private void AddWaveformPlaceholder(Canvas canvas, string message)
    {
        canvas.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = message,
            Foreground = CreateBrush("#8FA4C6"),
            FontSize = 12
        });
    }

    private void OnWaveformPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!TryParseWaveformEditorSteps(out var steps))
        {
            return;
        }

        var point = e.GetPosition(WaveformPreviewCanvas);
        if (!TryFindWaveformDragHandle(point, out var handle))
        {
            return;
        }

        _activeWaveformDrag = new WaveformDragSession(
            handle,
            point,
            steps,
            Math.Max(1d, WaveformPreviewCanvas.ActualWidth > 0 ? WaveformPreviewCanvas.ActualWidth : 520d),
            WaveformPreviewCanvas.Height);
        WaveformPreviewCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnWaveformPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_activeWaveformDrag is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var drag = _activeWaveformDrag;
        var point = e.GetPosition(WaveformPreviewCanvas);
        IReadOnlyList<EmsWaveformStep> updatedSteps = drag.Handle.Kind switch
        {
            WaveformDragHandleKind.ChannelA or WaveformDragHandleKind.ChannelB => WaveformDragEditorLogic.UpdateStrength(
                drag.OriginalSteps,
                drag.Handle.StepIndex,
                drag.Handle.Kind,
                point.Y,
                drag.Height,
                WaveformPreviewPadding),
            WaveformDragHandleKind.Duration => WaveformDragEditorLogic.UpdateDurationFromDelta(
                drag.OriginalSteps,
                drag.Handle.StepIndex,
                point.X - drag.StartPoint.X,
                drag.Width,
                WaveformPreviewPadding),
            _ => drag.OriginalSteps
        };

        UpdateWaveformScriptFromSteps(updatedSteps);
        e.Handled = true;
    }

    private void OnWaveformPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndWaveformPreviewDrag();
    }

    private void OnWaveformPreviewLostMouseCapture(object sender, MouseEventArgs e)
    {
        _activeWaveformDrag = null;
    }

    private void EndWaveformPreviewDrag()
    {
        _activeWaveformDrag = null;
        if (WaveformPreviewCanvas.IsMouseCaptured)
        {
            WaveformPreviewCanvas.ReleaseMouseCapture();
        }
    }

    private bool TryFindWaveformDragHandle(Point point, out WaveformDragHandle handle)
    {
        const double strengthHitRadius = 10d;
        const double durationHitRadius = 9d;

        foreach (var candidate in _waveformDragHandles.Reverse())
        {
            var hit = candidate.Kind switch
            {
                WaveformDragHandleKind.ChannelA or WaveformDragHandleKind.ChannelB =>
                    Math.Pow(point.X - candidate.X, 2) + Math.Pow(point.Y - candidate.Y, 2) <= strengthHitRadius * strengthHitRadius,
                WaveformDragHandleKind.Duration =>
                    Math.Abs(point.X - candidate.X) <= durationHitRadius && Math.Abs(point.Y - candidate.Y) <= durationHitRadius,
                _ => false
            };

            if (hit)
            {
                handle = candidate;
                return true;
            }
        }

        handle = null!;
        return false;
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

    private void SetWaveformEditorValues(string name, string script)
    {
        _isUpdatingWaveformEditor = true;
        try
        {
            WaveformNameTextBox.Text = name;
            WaveformScriptTextBox.Text = script;
        }
        finally
        {
            _isUpdatingWaveformEditor = false;
        }
    }

    private bool TryParseWaveformEditorSteps(out List<EmsWaveformStep> steps)
    {
        try
        {
            steps = WaveformScriptSerializer.Parse(WaveformScriptTextBox.Text);
            return true;
        }
        catch (FormatException)
        {
            steps = [];
            return false;
        }
    }

    private void UpdateWaveformScriptFromSteps(IReadOnlyList<EmsWaveformStep> steps)
    {
        SetWaveformEditorValues(WaveformNameTextBox.Text, WaveformScriptSerializer.Serialize(steps));
        RenderWaveformPreviewFromEditor();
        RefreshWaveformStepEditor();
    }

    private void RefreshWaveformStepEditor()
    {
        WaveformStepEditorPanel.Children.Clear();
        if (!TryParseWaveformEditorSteps(out var steps))
        {
            WaveformStepEditorPanel.Children.Add(new TextBlock
            {
                Text = "脚本格式无效，暂时无法显示步骤卡片。",
                Foreground = CreateBrush("#FCA5A5"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        for (var index = 0; index < steps.Count; index++)
        {
            WaveformStepEditorPanel.Children.Add(BuildWaveformStepCard(steps, index));
        }
    }

    private Border BuildWaveformStepCard(IReadOnlyList<EmsWaveformStep> steps, int index)
    {
        var step = steps[index];
        var card = new Border
        {
            Background = CreateBrush("#0E182A"),
            BorderBrush = CreateBrush("#22314B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var shell = new Grid();
        shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        shell.ColumnDefinitions.Add(new ColumnDefinition());
        card.Child = shell;

        shell.Children.Add(new Border
        {
            Background = ResolveStepAccentBrush(index),
            CornerRadius = new CornerRadius(18, 0, 0, 18)
        });

        var root = new StackPanel
        {
            Margin = new Thickness(16, 14, 16, 16)
        };
        Grid.SetColumn(root, 1);
        shell.Children.Add(root);

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.Children.Add(header);

        var titleStack = new StackPanel();
        header.Children.Add(titleStack);

        titleStack.Children.Add(new Border
        {
            Background = ResolveStepAccentBackgroundBrush(index),
            BorderBrush = ResolveStepAccentBrush(index),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4, 10, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = $"步骤 {index + 1}",
                Foreground = ResolveStepAccentBrush(index),
                FontSize = 11,
                FontWeight = FontWeights.Bold
            }
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"{step.DurationMs} ms  ·  A {step.AStrength}  ·  B {step.BStrength}",
            Margin = new Thickness(0, 10, 0, 0),
            Foreground = CreateBrush("#F8FAFC"),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"模式 A{step.AMode} / B{step.BMode} · 电机 {step.MotorState}",
            Margin = new Thickness(0, 6, 0, 0),
            Foreground = CreateBrush("#7F96B8"),
            FontSize = 11
        });

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        Grid.SetColumn(actionRow, 1);
        header.Children.Add(actionRow);

        actionRow.Children.Add(CreateStepActionButton("上移", (_, _) =>
        {
            UpdateWaveformScriptFromSteps(WaveformStepEditorLogic.MoveStep(GetCurrentEditorSteps(), index, moveUp: true));
        }, index <= 0));
        actionRow.Children.Add(CreateStepActionButton("下移", (_, _) =>
        {
            UpdateWaveformScriptFromSteps(WaveformStepEditorLogic.MoveStep(GetCurrentEditorSteps(), index, moveUp: false));
        }, index >= steps.Count - 1));
        actionRow.Children.Add(CreateStepActionButton("后插", (_, _) =>
        {
            UpdateWaveformScriptFromSteps(WaveformStepEditorLogic.InsertStepAfter(GetCurrentEditorSteps(), index));
        }));
        actionRow.Children.Add(CreateStepActionButton("删除", (_, _) =>
        {
            UpdateWaveformScriptFromSteps(WaveformStepEditorLogic.DeleteStep(GetCurrentEditorSteps(), index));
        }, isDisabled: false, isDanger: true));

        root.Children.Add(BuildStrengthSummaryRow(step));

        root.Children.Add(CreateStepFieldRow(
            ("时长", step.DurationMs, value => step = step with { DurationMs = value }),
            ("A 强度", step.AStrength, value => step = step with { AStrength = value }),
            ("A 模式", step.AMode, value => step = step with { AMode = value })));

        root.Children.Add(CreateStepFieldRow(
            ("B 强度", step.BStrength, value => step = step with { BStrength = value }),
            ("B 模式", step.BMode, value => step = step with { BMode = value }),
            ("电机", step.MotorState, value => step = step with { MotorState = value })));

        void Commit(Action<int> updater, string text)
        {
            if (!int.TryParse(text, out var value))
            {
                return;
            }

            updater(value);
            UpdateWaveformScriptFromSteps(WaveformStepEditorLogic.UpdateStep(GetCurrentEditorSteps(), index, step));
        }

        Grid CreateStepFieldRow(
            (string Label, int Value, Action<int> Update) first,
            (string Label, int Value, Action<int> Update) second,
            (string Label, int Value, Action<int> Update) third)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 10, 0, 0)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition());

            row.Children.Add(CreateStepFieldCell(first, 0));
            row.Children.Add(CreateStepFieldCell(second, 1));
            row.Children.Add(CreateStepFieldCell(third, 2));
            return row;
        }

        UIElement CreateStepFieldCell((string Label, int Value, Action<int> Update) field, int columnIndex)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(columnIndex == 0 ? 0 : 8, 0, 0, 0)
            };
            Grid.SetColumn(panel, columnIndex);

            panel.Children.Add(new TextBlock
            {
                Text = field.Label,
                Foreground = CreateBrush("#8FA4C6"),
                FontSize = 11
            });

            var textBox = new TextBox
            {
                Text = field.Value.ToString(),
                Margin = new Thickness(0, 6, 0, 0),
                Height = 34
            };
            textBox.LostFocus += (_, _) => Commit(field.Update, textBox.Text);
            textBox.KeyDown += (_, eventArgs) =>
            {
                if (eventArgs.Key == System.Windows.Input.Key.Enter)
                {
                    Commit(field.Update, textBox.Text);
                }
            };
            panel.Children.Add(textBox);
            return panel;
        }

        return card;
    }

    private IReadOnlyList<EmsWaveformStep> GetCurrentEditorSteps()
    {
        return TryParseWaveformEditorSteps(out var steps) ? steps : [new EmsWaveformStep()];
    }

    private Button CreateStepActionButton(string text, RoutedEventHandler handler, bool isDisabled = false)
    {
        return CreateStepActionButton(text, handler, isDisabled, isDanger: false);
    }

    private Button CreateStepActionButton(string text, RoutedEventHandler handler, bool isDisabled, bool isDanger)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            FontSize = 10,
            FontWeight = FontWeights.Medium,
            IsEnabled = !isDisabled
        };
        button.Background = isDanger ? CreateBrush("#261217") : CreateBrush("#101B2D");
        button.BorderBrush = isDanger ? CreateBrush("#6F3140") : CreateBrush("#233754");
        button.Foreground = isDanger ? CreateBrush("#F9C7D0") : CreateBrush("#C9D7EC");
        button.Click += handler;
        return button;
    }

    private UIElement BuildStrengthSummaryRow(EmsWaveformStep step)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 14, 0, 2)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "强度概览",
            Foreground = CreateBrush("#8FA4C6"),
            FontSize = 11
        });

        var row = new Grid
        {
            Margin = new Thickness(0, 10, 0, 0)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        row.ColumnDefinitions.Add(new ColumnDefinition());

        row.Children.Add(CreateStrengthBar("A 通道", step.AStrength, "#4FD1C5", 0));
        row.Children.Add(CreateStrengthBar("B 通道", step.BStrength, "#F59E0B", 2));
        panel.Children.Add(row);

        panel.Children.Add(new TextBlock
        {
            Text = "修改下列字段后，脚本文本和波形预览会自动同步。",
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = CreateBrush("#A9B8D3"),
            FontSize = 11
        });

        return panel;
    }

    private UIElement CreateStrengthBar(string label, int strength, string colorHex, int columnIndex)
    {
        var host = new StackPanel();
        Grid.SetColumn(host, columnIndex);

        host.Children.Add(new TextBlock
        {
            Text = $"{label}  {strength}%",
            Foreground = CreateBrush("#D8E2F3"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        });

        var track = new Border
        {
            Background = CreateBrush("#162235"),
            BorderBrush = CreateBrush("#263752"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Height = 12,
            Margin = new Thickness(0, 8, 0, 0),
            Child = new Grid()
        };

        var trackGrid = (Grid)track.Child;
        trackGrid.Children.Add(new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = Math.Max(10, 1.8 * Math.Clamp(strength, 0, 100)),
            Background = CreateBrush(colorHex),
            CornerRadius = new CornerRadius(2),
            Opacity = 0.95
        });

        host.Children.Add(track);
        return host;
    }

    private static Brush ResolveStepAccentBrush(int index)
    {
        return CreateBrush((index % 3) switch
        {
            0 => "#4FD1C5",
            1 => "#60A5FA",
            _ => "#F59E0B"
        });
    }

    private static Brush ResolveStepAccentBackgroundBrush(int index)
    {
        return CreateBrush((index % 3) switch
        {
            0 => "#11323B",
            1 => "#122A47",
            _ => "#3C2A11"
        });
    }

    private WaveformTriggerMode GetSelectedTriggerMode()
    {
        return (TriggerModeComboBox.SelectedItem as TriggerModeOption)?.Mode ?? WaveformTriggerMode.SpeedRules;
    }

    private sealed record WaveformDragSession(
        WaveformDragHandle Handle,
        Point StartPoint,
        IReadOnlyList<EmsWaveformStep> OriginalSteps,
        double Width,
        double Height);

    private sealed record TriggerModeOption(WaveformTriggerMode Mode, string Name);

    private sealed record DeviceOption(string DeviceId, string Summary);
}
