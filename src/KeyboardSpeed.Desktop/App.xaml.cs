using System.Windows;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Desktop;

public partial class App : Application
{
    private AppBootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _bootstrapper = new AppBootstrapper();
        _bootstrapper.Start();

        var mainWindow = new MainWindow(_bootstrapper);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _bootstrapper?.Dispose();
        base.OnExit(e);
    }
}
