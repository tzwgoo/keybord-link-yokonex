using System.Windows;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Typing;
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

    private sealed record DeviceOption(string DeviceId, string Summary);
}
