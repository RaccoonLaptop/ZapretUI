using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZapretUI.Controls.Backgrounds;

public abstract class AnimatedBackgroundBase : FrameworkElement
{
    public static double GlobalSpeed { get; set; } = 1.0;

    public double MotionSpeed { get; private set; } = 1.0;

    private const double MaxStepMs = 50;

    private double _lastTickMs;
    private double _simTimeMs;
    private bool _isRunning;

    protected double AreaWidth;
    protected double AreaHeight;
    protected readonly Random Rng = new();

    /// <summary>Дельта в секундах с учётом скорости.</summary>
    protected double DtSec(double deltaMs) => (deltaMs / 1000.0) * MotionSpeed;

    /// <summary>Дельта в миллисекундах симуляции (для метеоров и т.п.).</summary>
    protected double DtMs(double deltaMs) => deltaMs * MotionSpeed;

    protected AnimatedBackgroundBase()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        IsHitTestVisible = false;
        ClipToBounds = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => UpdateDimensions();
    }

    public void SetMotionSpeed(double speed)
    {
        speed = Math.Clamp(speed, 0.05, 5.0);
        if (Math.Abs(MotionSpeed - speed) < 1e-9)
            return;
        MotionSpeed = speed;
        OnMotionSpeedChanged();
        InvalidateVisual();
    }

    protected virtual void OnMotionSpeedChanged() { }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _lastTickMs = 0;
        _simTimeMs = 0;
        UpdateDimensions();
        StartLoop();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => StopLoop();

    protected void UpdateDimensions()
    {
        var w = ActualWidth > 1 ? ActualWidth : RenderSize.Width;
        var h = ActualHeight > 1 ? ActualHeight : RenderSize.Height;
        AreaWidth = w > 1 ? w : 800;
        AreaHeight = h > 1 ? h : 600;
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
        _lastTickMs = 0;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopLoop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _lastTickMs = 0;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isRunning || !IsVisible) return;

        if (AreaWidth <= 1 || AreaHeight <= 1)
            UpdateDimensions();
        if (AreaWidth <= 1 || AreaHeight <= 1) return;

        var now = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
        var deltaMs = _lastTickMs > 0 ? now - _lastTickMs : 1000.0 / 60.0;
        _lastTickMs = now;
        deltaMs = Math.Clamp(deltaMs, 1, MaxStepMs);

        _simTimeMs += deltaMs * MotionSpeed;
        AnimateFrame(_simTimeMs, deltaMs);
        InvalidateVisual();
    }

    protected virtual void AnimateFrame(double timeMs, double deltaMs) { }

    protected override void OnRender(DrawingContext dc) => RenderFrame(dc, _simTimeMs);

    protected abstract void RenderFrame(DrawingContext dc, double timeMs);

    protected static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    protected static void DrawCircle(DrawingContext dc, Color color, double opacity, Point center, double radius)
    {
        var brush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B));
        brush.Freeze();
        dc.DrawEllipse(brush, null, center, radius, radius);
    }
}
