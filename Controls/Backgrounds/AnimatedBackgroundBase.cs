using System.Windows;
using System.Windows.Media;

namespace ZapretUI.Controls.Backgrounds;

public abstract class AnimatedBackgroundBase : FrameworkElement
{
    protected double AreaWidth;
    protected double AreaHeight;
    protected readonly Random Rng = new();
    protected double StartMs;
    private bool _isRunning;

    protected AnimatedBackgroundBase()
    {
        Loaded += (_, _) =>
        {
            StartMs = NowMs();
            StartLoop();
        };
        Unloaded += (_, _) => StopLoop();
        SizeChanged += (_, _) => UpdateDimensions();
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    protected void UpdateDimensions()
    {
        AreaWidth = ActualWidth > 1 ? ActualWidth : 800;
        AreaHeight = ActualHeight > 1 ? ActualHeight : 600;
        OnDimensionsChanged();
    }

    protected virtual void OnDimensionsChanged() { }

    private void StartLoop()
    {
        if (_isRunning) return;
        _isRunning = true;
        UpdateDimensions();
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopLoop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isRunning || ActualWidth <= 1 || ActualHeight <= 1) return;
        if (Math.Abs(AreaWidth - ActualWidth) > 1 || Math.Abs(AreaHeight - ActualHeight) > 1)
            UpdateDimensions();
        AnimateFrame(NowMs() - StartMs);
        InvalidateVisual();
    }

    protected virtual void AnimateFrame(double timeMs) { }

    protected override void OnRender(DrawingContext dc) => RenderFrame(dc, NowMs() - StartMs);

    protected abstract void RenderFrame(DrawingContext dc, double timeMs);

    protected static double NowMs() => (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

    protected static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    protected static void DrawCircle(DrawingContext dc, Color color, double opacity, Point center, double radius)
    {
        var brush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B));
        brush.Freeze();
        dc.DrawEllipse(brush, null, center, radius, radius);
    }
}
