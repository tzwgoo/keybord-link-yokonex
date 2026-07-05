using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Desktop;

public partial class FloatingTelemetryWindow : Window
{
    private const double PreviewPadding = 12d;
    private const double ChannelBarMaxWidth = 132d;

    public FloatingTelemetryWindow()
    {
        InitializeComponent();
    }

    public void ApplyState(Services.FloatingTelemetryState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        DeviceNameTextBlock.Text = state.DeviceName;
        ConnectionStatusTextBlock.Text = state.ConnectionText;
        CharactersPerMinuteTextBlock.Text = state.CharactersPerMinuteText;
        CurrentWaveformTextBlock.Text = state.WaveformName;
        ChannelATextBlock.Text = FormatStrengthDisplay(state.ChannelAStrength);
        ChannelBTextBlock.Text = FormatStrengthDisplay(state.ChannelBStrength);
        ChannelABar.Width = ChannelBarMaxWidth * GetStrengthRatio(state.ChannelAStrength);
        ChannelBBar.Width = ChannelBarMaxWidth * GetStrengthRatio(state.ChannelBStrength);
        RenderWaveform(state.Waveform, state.ChannelAStrength, state.ChannelBStrength);
    }

    private void RenderWaveform(EmsWaveformDefinition? waveform, int channelAStrength, int channelBStrength)
    {
        WaveformCanvas.Children.Clear();
        var width = Math.Max(1d, WaveformCanvas.ActualWidth > 0 ? WaveformCanvas.ActualWidth : 324d);
        var height = WaveformCanvas.Height;

        if (waveform is null)
        {
            AddPlaceholder("等待波形触发");
            return;
        }

        var preview = WaveformPreviewBuilder.Build(waveform);
        if (preview.Points.Count == 0 || preview.TotalDurationMs <= 0)
        {
            AddPlaceholder("当前波形没有预览数据");
            return;
        }

        DrawGuides(width, height);

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

        foreach (var point in preview.Points)
        {
            var x = PreviewPadding + (width - PreviewPadding * 2) * point.TimeMs / preview.TotalDurationMs;
            var aY = height - PreviewPadding - (height - PreviewPadding * 2) * GetStrengthRatio(point.AStrength);
            var bY = height - PreviewPadding - (height - PreviewPadding * 2) * GetStrengthRatio(point.BStrength);
            aLine.Points.Add(new Point(x, aY));
            bLine.Points.Add(new Point(x, bY));
        }

        WaveformCanvas.Children.Add(aLine);
        WaveformCanvas.Children.Add(bLine);
        DrawLiveStrengthIndicator(width, height, channelAStrength, "#4FD1C5", 0d);
        DrawLiveStrengthIndicator(width, height, channelBStrength, "#F59E0B", 12d);
    }

    private void DrawGuides(double width, double height)
    {
        for (var index = 0; index < 4; index++)
        {
            var y = PreviewPadding + (height - PreviewPadding * 2) * index / 3d;
            WaveformCanvas.Children.Add(new Line
            {
                X1 = PreviewPadding,
                X2 = width - PreviewPadding,
                Y1 = y,
                Y2 = y,
                Stroke = CreateBrush("#1E2D48"),
                StrokeThickness = 1
            });
        }
    }

    private void AddPlaceholder(string text)
    {
        var textBlock = new System.Windows.Controls.TextBlock
        {
            Text = text,
            Foreground = CreateBrush("#8FA4C6"),
            FontSize = 12
        };
        WaveformCanvas.Children.Add(textBlock);
        System.Windows.Controls.Canvas.SetLeft(textBlock, 10d);
        System.Windows.Controls.Canvas.SetTop(textBlock, Math.Max(8d, WaveformCanvas.Height / 2d - 10d));
    }

    private void DrawLiveStrengthIndicator(double width, double height, int strength, string color, double xOffset)
    {
        var y = height - PreviewPadding - (height - PreviewPadding * 2) * GetStrengthRatio(strength);
        var x = width - PreviewPadding - 16d + xOffset;

        WaveformCanvas.Children.Add(new Line
        {
            X1 = PreviewPadding,
            X2 = width - PreviewPadding,
            Y1 = y,
            Y2 = y,
            Stroke = CreateBrush(color),
            StrokeThickness = 1,
            Opacity = 0.22
        });

        var marker = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = CreateBrush(color),
            Stroke = CreateBrush("#E7F0FF"),
            StrokeThickness = 1
        };
        WaveformCanvas.Children.Add(marker);
        System.Windows.Controls.Canvas.SetLeft(marker, x);
        System.Windows.Controls.Canvas.SetTop(marker, y - 4d);
    }

    private void OnShellMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        DragMove();
    }

    private static Brush CreateBrush(string hexColor)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor)!;
    }

    private static double GetStrengthRatio(int strength)
    {
        return EmsWaveformStep.ClampStrength(strength) / (double)EmsWaveformStep.MaxStrength;
    }

    private static string FormatStrengthDisplay(int strength)
    {
        return $"{strength}/{EmsWaveformStep.MaxStrength}";
    }
}
