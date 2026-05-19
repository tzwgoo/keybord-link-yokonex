using System.Windows;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Diagnostics;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Core.Waveforms;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Desktop;

public partial class App : Application
{
    private AppBootstrapper? _bootstrapper;
    private FloatingTelemetryWindow? _floatingTelemetryWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        try
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            AppDiagnostics.WriteInfo("App.OnStartup", $"应用启动，日志文件: {AppDiagnostics.LogFilePath}");

            _bootstrapper = new AppBootstrapper();
            AppDiagnostics.WriteInfo("App.OnStartup", "AppBootstrapper 构建完成");

            _bootstrapper.Start();
            AppDiagnostics.WriteInfo("App.OnStartup", "AppBootstrapper 已启动");

            var mainWindow = new MainWindow(_bootstrapper);
            MainWindow = mainWindow;
            mainWindow.Show();
            AppDiagnostics.WriteInfo("App.OnStartup", "主窗口已显示");

            _floatingTelemetryWindow = new FloatingTelemetryWindow();
            _bootstrapper.SnapshotUpdated += HandleSnapshotUpdated;
            _bootstrapper.BluetoothStatusUpdated += HandleBluetoothStatusUpdated;
            RefreshFloatingTelemetryWindow(_bootstrapper.CurrentSnapshot, _bootstrapper.BluetoothStatus);
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("App.OnStartup", ex);
            MessageBox.Show(
                $"程序在启动阶段发生异常，已写入日志：{AppDiagnostics.LogFilePath}{Environment.NewLine}{ex.Message}",
                "Keyboard Speed YOKONEX",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppDiagnostics.WriteInfo("App.OnExit", $"应用退出，退出代码: {e.ApplicationExitCode}");
        if (_bootstrapper is not null)
        {
            _bootstrapper.SnapshotUpdated -= HandleSnapshotUpdated;
            _bootstrapper.BluetoothStatusUpdated -= HandleBluetoothStatusUpdated;
        }
        _floatingTelemetryWindow?.Close();
        _bootstrapper?.Dispose();
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        AppDiagnostics.WriteException("App.DispatcherUnhandledException", e.Exception);
        e.Handled = true;
        MessageBox.Show(
            $"程序捕获到未处理异常，已写入日志：{AppDiagnostics.LogFilePath}{Environment.NewLine}{e.Exception.Message}",
            "Keyboard Speed YOKONEX",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            AppDiagnostics.WriteException("AppDomain.CurrentDomain.UnhandledException", exception);
            return;
        }

        AppDiagnostics.WriteInfo("AppDomain.CurrentDomain.UnhandledException", $"非托管异常对象: {e.ExceptionObject}");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppDiagnostics.WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private void HandleSnapshotUpdated(TypingSpeedSnapshot snapshot)
    {
        if (_bootstrapper is null)
        {
            return;
        }

        Dispatcher.Invoke(() => RefreshFloatingTelemetryWindow(snapshot, _bootstrapper.BluetoothStatus));
    }

    private void HandleBluetoothStatusUpdated(BluetoothConnectionStatus status)
    {
        if (_bootstrapper is null)
        {
            return;
        }

        Dispatcher.Invoke(() => RefreshFloatingTelemetryWindow(_bootstrapper.CurrentSnapshot, status));
    }

    private void RefreshFloatingTelemetryWindow(TypingSpeedSnapshot snapshot, BluetoothConnectionStatus status)
    {
        if (_bootstrapper is null || _floatingTelemetryWindow is null)
        {
            return;
        }

        var state = FloatingTelemetryPresenter.BuildState(
            snapshot,
            status,
            _bootstrapper.CurrentRuleName,
            ResolveCurrentWaveform(),
            _bootstrapper.CurrentWaveformName);

        _floatingTelemetryWindow.ApplyState(state);

        if (state.IsVisible)
        {
            if (!_floatingTelemetryWindow.IsVisible)
            {
                _floatingTelemetryWindow.Show();
            }
        }
        else if (_floatingTelemetryWindow.IsVisible)
        {
            _floatingTelemetryWindow.Hide();
        }
    }

    private EmsWaveformDefinition? ResolveCurrentWaveform()
    {
        if (_bootstrapper is null || string.IsNullOrWhiteSpace(_bootstrapper.CurrentWaveformName))
        {
            return null;
        }

        return _bootstrapper.Waveforms.FirstOrDefault(item =>
            string.Equals(item.Name, _bootstrapper.CurrentWaveformName, StringComparison.Ordinal));
    }
}
