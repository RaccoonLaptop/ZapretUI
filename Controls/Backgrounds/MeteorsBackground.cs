using System.Windows;
using System.Windows.Media;

namespace ZapretUI.Controls.Backgrounds;

/// <summary>
/// CSS-метеоры как в bedolaga-cabinet (duration 2–8s, translate ~500px, угол ~215°).
/// https://github.com/BEDOLAGA-DEV/bedolaga-cabinet/blob/main/src/components/ui/backgrounds/meteors.tsx
/// </summary>
public sealed class MeteorsBackground : AnimatedBackgroundBase
{
    private const double TravelPx = 500;
    private const double AngleRad = 215 * Math.PI / 180;

    private readonly List<Meteor> _meteors = new();

    private sealed class Meteor
    {
        public double StartX, StartY;
        public double DurationMs;
        public double DelayMs;
        public double ElapsedMs;
        public double Size;
        public bool Started;
    }

    protected override void OnDimensionsChanged()
    {
        _meteors.Clear();
        if (AreaWidth <= 0 || AreaHeight <= 0) return;

        var count = Math.Clamp(20, 1, 50);
        for (var i = 0; i < count; i++)
            _meteors.Add(CreateMeteor());
    }

    protected override void AnimateFrame(double timeMs, double deltaMs)
    {
        if (AreaWidth <= 0 || AreaHeight <= 0) return;

        foreach (var m in _meteors)
        {
            if (!m.Started)
            {
                m.DelayMs -= deltaMs;
                if (m.DelayMs <= 0)
                {
                    m.Started = true;
                    m.ElapsedMs = 0;
                }
                continue;
            }

            m.ElapsedMs += deltaMs;
            if (m.ElapsedMs >= m.DurationMs)
                ResetMeteor(m);
        }
    }

    protected override void RenderFrame(DrawingContext dc, double timeMs)
    {
        if (AreaWidth <= 0 || AreaHeight <= 0) return;

        foreach (var m in _meteors)
        {
            if (!m.Started) continue;

            var t = m.ElapsedMs / m.DurationMs;
            if (t > 1) continue;

            // bedolaga: 0–70% opacity 1, затем затухание
            var opacity = t < 0.7 ? 1.0 : 1.0 - (t - 0.7) / 0.3;

            var dist = t * TravelPx;
            // CSS: rotate(215deg) translateX(-500px) — движение вниз-влево
            var headX = m.StartX + Math.Cos(AngleRad) * dist;
            var headY = m.StartY - Math.Sin(AngleRad) * dist;
            var tailLen = Math.Min(80, dist);
            var tailX = headX - Math.Cos(AngleRad) * tailLen;
            var tailY = headY + Math.Sin(AngleRad) * tailLen;

            var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 200), 255, 255, 255)), m.Size)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();
            dc.DrawLine(pen, new Point(tailX, tailY), new Point(headX, headY));
        }
    }

    private Meteor CreateMeteor() => new()
    {
        StartX = Rng.NextDouble() * AreaWidth,
        StartY = -10 - Rng.NextDouble() * AreaHeight * 0.08,
        DurationMs = 2000 + Rng.NextDouble() * 6000,
        DelayMs = Rng.NextDouble() * 5000,
        Size = 1 + Rng.NextDouble(),
        Started = false
    };

    private void ResetMeteor(Meteor m)
    {
        m.StartX = Rng.NextDouble() * AreaWidth;
        m.StartY = -10 - Rng.NextDouble() * AreaHeight * 0.08;
        m.DurationMs = 2000 + Rng.NextDouble() * 6000;
        m.DelayMs = Rng.NextDouble() * 2000;
        m.ElapsedMs = 0;
        m.Started = false;
    }
}
