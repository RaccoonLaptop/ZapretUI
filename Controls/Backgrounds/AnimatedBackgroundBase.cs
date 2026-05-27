using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZapretUI.Controls.Backgrounds;

public abstract class AnimatedBackgroundBase : FrameworkElement
{
    /// <summary>
    /// Множитель для canvas-фонов (звёзды, метеоры, искры, вихрь).
    /// Bedolaga-константы на WPF выглядят быстрее — снижаем ~в 3 раза.
    /// </summary>
    protected const double CanvasMotionScale = 0.32;

    /// <summary>Доп. замедление для фонов, где время задаётся в RenderFrame.</summary>
    protected const double MotionScale = 0.45;

    private const double TargetFps = 60;
    private const double FrameDeltaMs = 1000.0 / TargetFps;

    private DispatcherTimer? _timer;
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
        UpdateDimensions();

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(FrameDeltaMs)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void StopLoop()
    {
        if (!_isRunning) return;
        _isRunning = false;
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

        AnimateFrame(NowMs() - StartMs, FrameDeltaMs);
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
