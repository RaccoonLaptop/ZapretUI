using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ZapretUI.Controls;

public sealed class ShootingStarsBackground : Canvas
{
    private readonly List<Star> _stars = new();
    private readonly List<ShootingStar> _shootingStars = new();
    private readonly DispatcherTimer _timer;
    private readonly Random _rng = new();
    private double _width;
    private double _height;

    private sealed class Star
    {
        public double X, Y, Size, Opacity, TwinklePhase;
    }

    private sealed class ShootingStar
    {
        public double X, Y, Vx, Vy, Opacity;
    }

    public ShootingStarsBackground()
    {
        Loaded += OnLoaded;
        SizeChanged += (_, _) => OnResize();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => OnTick();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OnResize();
        _timer.Start();
    }

    private void OnResize()
    {
        _width = ActualWidth > 1 ? ActualWidth : 800;
        _height = ActualHeight > 1 ? ActualHeight : 600;
        if (_stars.Count == 0)
            InitStars();
    }

    private void InitStars()
    {
        _stars.Clear();
        var count = Math.Clamp((int)(_width * _height / 12000), 80, 220);
        for (var i = 0; i < count; i++)
        {
            _stars.Add(new Star
            {
                X = _rng.NextDouble() * _width,
                Y = _rng.NextDouble() * _height,
                Size = 0.8 + _rng.NextDouble() * 1.8,
                Opacity = 0.15 + _rng.NextDouble() * 0.55,
                TwinklePhase = _rng.NextDouble() * Math.PI * 2
            });
        }
    }

    private void OnTick()
    {
        if (_width <= 0 || _height <= 0) return;

        foreach (var s in _stars)
        {
            s.TwinklePhase += 0.04;
            if (s.TwinklePhase > Math.PI * 2)
                s.TwinklePhase -= Math.PI * 2;
        }

        if (_rng.NextDouble() < 0.08)
            SpawnShootingStar();

        for (var i = _shootingStars.Count - 1; i >= 0; i--)
        {
            var sh = _shootingStars[i];
            sh.X += sh.Vx;
            sh.Y += sh.Vy;
            sh.Opacity -= 0.018;
            if (sh.X > _width + 80 || sh.Y > _height + 40 || sh.Opacity <= 0)
                _shootingStars.RemoveAt(i);
        }

        InvalidateVisual();
    }

    private void SpawnShootingStar()
    {
        var angle = _rng.NextDouble() * 0.5 + 0.2;
        var speed = 8 + _rng.NextDouble() * 14;
        _shootingStars.Add(new ShootingStar
        {
            X = -40 - _rng.NextDouble() * 60,
            Y = _rng.NextDouble() * _height * 0.4,
            Vx = Math.Cos(angle) * speed,
            Vy = Math.Sin(angle) * speed + 2,
            Opacity = 0.85 + _rng.NextDouble() * 0.15
        });
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_width <= 0 || _height <= 0) return;

        foreach (var s in _stars)
        {
            var twinkle = 0.65 + 0.35 * Math.Sin(s.TwinklePhase);
            var brush = new SolidColorBrush(Color.FromArgb(
                (byte)(s.Opacity * twinkle * 255), 220, 230, 255));
            brush.Freeze();
            dc.DrawEllipse(brush, null, new Point(s.X, s.Y), s.Size, s.Size);
        }

        foreach (var sh in _shootingStars)
        {
            var head = new Point(sh.X, sh.Y);
            var tail = new Point(sh.X - sh.Vx * 4, sh.Y - sh.Vy * 4);
            var brush = new SolidColorBrush(Color.FromArgb((byte)(sh.Opacity * 255), 200, 220, 255));
            brush.Freeze();
            var pen = new Pen(brush, 1.8)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();
            dc.DrawLine(pen, tail, head);
        }
    }
}
