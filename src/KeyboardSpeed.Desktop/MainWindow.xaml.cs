using System.Windows;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Desktop.Services;

namespace KeyboardSpeed.Desktop;

public partial class MainWindow : Window
{
    private readonly AppBootstrapper _bootstrapper;

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
        ApplySnapshot(_bootstrapper.CurrentSnapshot);
        StatusText.Text = _bootstrapper.IsListening ? "监听中" : "未监听";
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _bootstrapper.SnapshotUpdated -= HandleSnapshotUpdated;
    }

    private void HandleSnapshotUpdated(TypingSpeedSnapshot snapshot)
    {
        Dispatcher.Invoke(() => ApplySnapshot(snapshot));
    }

    private void ApplySnapshot(TypingSpeedSnapshot snapshot)
    {
        KpmText.Text = snapshot.RealtimeKpm.ToString("0.0");
        WpmText.Text = snapshot.RealtimeWpm.ToString("0.0");
        SamplesText.Text = snapshot.ActiveSampleCount.ToString();
        TrendText.Text = snapshot.TrendKpm.ToString("0.0");
        LastKeyText.Text = _bootstrapper.LastKeystrokeAt?.ToLocalTime().ToString("HH:mm:ss") ?? "--:--:--";
    }
}
