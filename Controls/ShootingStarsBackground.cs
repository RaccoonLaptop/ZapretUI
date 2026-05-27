using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace ZapretUI.Controls;

/// <summary>
/// Shooting stars background (ported from bedolaga-cabinet shooting-stars.tsx).
/// https://github.com/BEDOLAGA-DEV/bedolaga-cabinet/blob/main/src/components/ui/backgrounds/shooting-stars.tsx
/// </summary>
public sealed class ShootingStarsBackground : Canvas
{
    private const double StarDensity = 0.00015;
    private const double MinSpeed = 10;
    private const double MaxSpeed = 30;
    private const double TrailLength = 80;
    private const double FadeDistance = 500;

    private static readonly Color StarColor = (Color)ColorConverter.ConvertFromString("#9E00FF")!;
    private static readonly Color TrailColor = (Color)ColorConverter.ConvertFromString("#2EB9DF")!;

    private readonly List<BgStar> _bgStars = new();
    private readonly List<ShootingStar> _shootingStars = new();
    private readonly Random _rng = new();

    private double _width;
    private double _height;
    private double _lastShootingMs;
    private double _nextShootingDelayMs;
    private double _startMs;
    private bool _isRendering;

    private sealed class BgStar
    {
        public double X, Y, Radius, Opacity;
        public double? TwinkleSpeed;
    }

    private sealed class ShootingStar
    {
        public double X, Y, Angle, Scale, Speed, Distance, Opacity;
    }

    public ShootingStarsBackground()
    {
        _nextShootingDelayMs = 4200 + _rng.NextDouble() * 4500;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => RebuildBackgroundStars();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _startMs = GetTimeMs();
        _lastShootingMs = _startMs;
        UpdateSize();
        RebuildBackgroundStars();
        StartLoop();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => StopLoop();

    private void StartLoop()
    {
        if (_isRendering) return;
        _isRendering = true;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopLoop()
    {
        if (!_isRendering) return;
        _isRendering = false;
        CompositionTarget.Rendering -= OnRendering;
    }

    private void UpdateSize()
    {
        _width = ActualWidth > 1 ? ActualWidth : 800;
        _height = ActualHeight > 1 ? ActualHeight : 600;
    }

    private void RebuildBackgroundStars()
    {
        UpdateSize();
        if (_width <= 0 || _height <= 0) return;

        _bgStars.Clear();
        var count = Math.Max(1, (int)(_width * _height * StarDensity));
        for (var i = 0; i < count; i++)
        {
            _bgStars.Add(new BgStar
            {
                X = _rng.NextDouble() * _width,
                Y = _rng.NextDouble() * _height,
                Radius = _rng.NextDouble() * 1.2 + 0.3,
                Opacity = _rng.NextDouble(),
                TwinkleSpeed = _rng.NextDouble() > 0.3 ? 0.5 + _rng.NextDouble() * 0.5 : null
            });
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_width <= 0 || _height <= 0) return;

        var timeMs = GetTimeMs();

        if (timeMs - _lastShootingMs > _nextShootingDelayMs)
        {
            _shootingStars.Add(new ShootingStar
            {
                X = _rng.NextDouble() * _width,
                Y = _rng.NextDouble() * _height * 0.5,
                Angle = Math.PI / 4 + (_rng.NextDouble() - 0.5) * 0.3,
                Scale = 0.5 + _rng.NextDouble() * 0.5,
                Speed = MinSpeed + _rng.NextDouble() * (MaxSpeed - MinSpeed),
                Distance = 0,
                Opacity = 1
            });
            _lastShootingMs = timeMs;
            _nextShootingDelayMs = 4200 + _rng.NextDouble() * 4500;
        }

        for (var i = _shootingStars.Count - 1; i >= 0; i--)
        {
            var star = _shootingStars[i];
            star.Distance += star.Speed;
            star.Opacity = Math.Max(0, 1 - star.Distance / FadeDistance);
            if (star.Opacity <= 0)
                _shootingStars.RemoveAt(i);
        }

        InvalidateVisual();
    }

    private double GetTimeMs() =>
        (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

    protected override void OnRender(DrawingContext dc)
    {
        if (_width <= 0 || _height <= 0) return;

        var timeSec = (GetTimeMs() - _startMs) / 1000.0;

        foreach (var s in _bgStars)
        {
            var opacity = s.Opacity;
            if (s.TwinkleSpeed is { } speed)
                opacity = 0.5 + 0.5 * Math.Sin(timeSec * speed * Math.PI * 2);

            var brush = new SolidColorBrush(Color.FromArgb(
                (byte)(opacity * 255), 255, 255, 255));
            brush.Freeze();
            dc.DrawEllipse(brush, null, new Point(s.X, s.Y), s.Radius, s.Radius);
        }

        foreach (var star in _shootingStars)
        {
            var x2 = star.X + Math.Cos(star.Angle) * star.Distance;
            var y2 = star.Y + Math.Sin(star.Angle) * star.Distance;
            var tailDist = Math.Max(0, star.Distance - TrailLength);
            var tailX = star.X + Math.Cos(star.Angle) * tailDist;
            var tailY = star.Y + Math.Sin(star.Angle) * tailDist;

            var trailBrush = new SolidColorBrush(Color.FromArgb(
                (byte)(star.Opacity * 0.4 * 255), TrailColor.R, TrailColor.G, TrailColor.B));
            trailBrush.Freeze();
            var trailPen = new Pen(trailBrush, star.Scale * 2)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            trailPen.Freeze();
            dc.DrawLine(trailPen, new Point(tailX, tailY), new Point(x2, y2));

            var headBrush = new SolidColorBrush(Color.FromArgb(
                (byte)(star.Opacity * 255), StarColor.R, StarColor.G, StarColor.B));
            headBrush.Freeze();
            dc.DrawEllipse(headBrush, null, new Point(x2, y2), star.Scale * 1.5, star.Scale * 1.5);
        }
    }
}
