using System.Windows;
using KeyboardSpeed.Core.Diagnostics;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Desktop;

public partial class App : Application
{
    private AppBootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _bootstrapper = new AppBootstrapper();
        _bootstrapper.Start();

        var mainWindow = new MainWindow(_bootstrapper);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
}
