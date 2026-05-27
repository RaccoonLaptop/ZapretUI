using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZapretUI.Controls.Backgrounds;

public abstract class AnimatedBackgroundBase : FrameworkElement
{
    /// <summary>Синхронизация с настройками (ползунок на «Главной»).</summary>
    public static double GlobalSpeed { get; set; } = 0.15;

    /// <summary>Скорость этого экземпляра фона (0.05…1.5).</summary>
    public double MotionSpeed { get; set; } = 0.15;

    private const double MaxStepMs = 50;

    private DispatcherTimer? _timer;
    private double _lastTickMs;
    /// <summary>Симулированное время (мс); растёт с учётом <see cref="MotionSpeed"/>.</summary>
    private double _simTimeMs;
    protected double AreaWidth;
    protected double AreaHeight;
    protected readonly Random Rng = new();
    protected double StartMs;
    private bool _isRunning;

    protected double DtSec(double deltaMs) => (deltaMs / 1000.0) * MotionSpeed;

    protected AnimatedBackgroundBase()
    {
        Loaded += (_, _) =>
        {
            StartMs = NowMs();
            _lastTickMs = 0;
            _simTimeMs = 0;
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
        _lastTickMs = 0;
        UpdateDimensions();

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void StopLoop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _lastTickMs = 0;
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_isRunning || ActualWidth <= 1 || ActualHeight <= 1) return;
        if (Math.Abs(AreaWidth - ActualWidth) > 1 || Math.Abs(AreaHeight - ActualHeight) > 1)
            UpdateDimensions();

        var now = NowMs();
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

    protected static double NowMs() => (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

    protected static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    protected static void DrawCircle(DrawingContext dc, Color color, double opacity, Point center, double radius)
    {
        var brush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B));
        brush.Freeze();
        dc.DrawEllipse(brush, null, center, radius, radius);
    }
}
