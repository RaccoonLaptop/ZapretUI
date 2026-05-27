using System.Windows;
using System.Windows.Media;

namespace ZapretUI.Controls.Backgrounds;

public abstract class AnimatedBackgroundBase : FrameworkElement
{
    /// <summary>Доп. замедление для фонов, где время задаётся в RenderFrame (не в AnimateFrame).</summary>
    protected const double MotionScale = 0.45;

    /// <summary>Как useAnimationLoop в bedolaga-cabinet: не чаще 60 FPS.</summary>
    private const double TargetFps = 60;
    private const double FrameIntervalMs = 1000.0 / TargetFps;
    private TimeSpan _lastRenderingTime;

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

    protected override Size MeasureOverride(Size availableSize) => availableSize;

    protected override Size ArrangeOverride(Size finalSize)
    {
        AreaWidth = finalSize.Width > 1 ? finalSize.Width : AreaWidth;
        AreaHeight = finalSize.Height > 1 ? finalSize.Height : AreaHeight;
        if (_isRunning && AreaWidth > 1 && AreaHeight > 1)
            OnDimensionsChanged();
        return finalSize;
    }

    private void StartLoop()
    {
        if (_isRunning) return;
        _isRunning = true;
        _lastRenderingTime = default;
        UpdateDimensions();
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopLoop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _lastRenderingTime = default;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isRunning || ActualWidth <= 1 || ActualHeight <= 1) return;
        if (Math.Abs(AreaWidth - ActualWidth) > 1 || Math.Abs(AreaHeight - ActualHeight) > 1)
            UpdateDimensions();

        var args = (RenderingEventArgs)e!;
        var renderTime = args.RenderingTime;

        if (_lastRenderingTime != default)
        {
            var sinceLast = (renderTime - _lastRenderingTime).TotalMilliseconds;
            if (sinceLast < FrameIntervalMs - 0.5)
                return;
        }

        var deltaMs = _lastRenderingTime == default
            ? FrameIntervalMs
            : Math.Clamp((renderTime - _lastRenderingTime).TotalMilliseconds, FrameIntervalMs, 48);
        _lastRenderingTime = renderTime;

        AnimateFrame(NowMs() - StartMs, deltaMs);
        InvalidateVisual();
    }

    protected virtual void AnimateFrame(double timeMs, double deltaMs) { }

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
