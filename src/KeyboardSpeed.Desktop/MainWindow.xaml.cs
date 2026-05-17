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
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _bootstrapper.SnapshotUpdated += HandleSnapshotUpdated;
        _bootstrapper.BluetoothStatusUpdated += HandleBluetoothStatusUpdated;
        ApplySnapshot(_bootstrapper.CurrentSnapshot);
        StatusText.Text = _bootstrapper.IsListening ? "监听中" : "未监听";
        ApplyBluetoothStatus(_bootstrapper.BluetoothStatus);
        RefreshDeviceList();
        RefreshWaveformList();
        RefreshRuleList();
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
        });
    }

    private void ApplySnapshot(TypingSpeedSnapshot snapshot)
    {
        KpmText.Text = snapshot.RealtimeKpm.ToString("0.0");
        WpmText.Text = snapshot.RealtimeWpm.ToString("0.0");
        SamplesText.Text = snapshot.ActiveSampleCount.ToString();
        TrendText.Text = snapshot.TrendKpm.ToString("0.0");
        LastKeyText.Text = _bootstrapper.LastKeystrokeAt?.ToLocalTime().ToString("HH:mm:ss") ?? "--:--:--";
        UpdateRuleState();
    }

    private void ApplyBluetoothStatus(BluetoothConnectionStatus status)
    {
        DeviceStatusText.Text = status.IsConnected
            ? $"已连接: {status.Device?.Name ?? "未知设备"}"
            : "未连接";
        BatteryText.Text = $"电量: {(status.BatteryLevel.HasValue ? $"{status.BatteryLevel.Value}%" : "--")}";
        ErrorText.Text = $"最近错误: {(string.IsNullOrWhiteSpace(status.LastError) ? "无" : status.LastError)}";
        ConnectButton.IsEnabled = !_isBusy;
        DisconnectButton.IsEnabled = !_isBusy && status.IsConnected;
        StopButton.IsEnabled = !_isBusy && status.IsConnected;
        RefreshButton.IsEnabled = !_isBusy;
        ScanButton.IsEnabled = !_isBusy;
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
        RenderWaveformPreview(WaveformsComboBox.SelectedItem as EmsWaveformDefinition);
        ApplyWaveformEditor(WaveformsComboBox.SelectedItem as EmsWaveformDefinition);
    }

    private void RefreshRuleList()
    {
        var selectedRuleId = (RulesComboBox.SelectedItem as SpeedRangeRule)?.Id;
        var rules = _bootstrapper.SpeedRules.ToList();
        RulesComboBox.ItemsSource = rules;
        RulesComboBox.SelectedItem = rules.FirstOrDefault(item => item.Id == selectedRuleId)
            ?? rules.FirstOrDefault();
        ApplyRuleEditor(RulesComboBox.SelectedItem as SpeedRangeRule);
    }

    private void UpdateRuleState()
    {
        RuleText.Text = $"命中规则: {_bootstrapper.CurrentRuleName}";
        WaveformText.Text = $"当前波形: {_bootstrapper.CurrentWaveformName}";
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
        RenderWaveformPreview(waveform);
        ApplyWaveformEditor(waveform);
    }

    private void OnRuleSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ApplyRuleEditor(RulesComboBox.SelectedItem as SpeedRangeRule);
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
            return;
        }

        var preview = WaveformPreviewBuilder.Build(waveform);
        if (preview.Points.Count == 0 || preview.TotalDurationMs <= 0)
        {
            return;
        }

        var aLine = new Polyline
        {
            Stroke = Brushes.Cyan,
            StrokeThickness = 2
        };
        var bLine = new Polyline
        {
            Stroke = Brushes.Orange,
            StrokeThickness = 2
        };

        const double padding = 8d;
        var width = Math.Max(1d, WaveformPreviewCanvas.ActualWidth > 0 ? WaveformPreviewCanvas.ActualWidth : WaveformPreviewCanvas.Width);
        if (width <= 0)
        {
            width = 280d;
        }

        var height = WaveformPreviewCanvas.Height;
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
    }

    private void ApplyWaveformEditor(EmsWaveformDefinition? waveform)
    {
        WaveformNameTextBox.Text = waveform?.Name ?? "新波形";
        WaveformScriptTextBox.Text = waveform is null
            ? "120,10,1,10,1,0"
            : WaveformScriptSerializer.Serialize(waveform.Steps);
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

    private sealed record DeviceOption(string DeviceId, string Summary);
}
