using System.Windows;
using System.Windows.Controls;
using ZapretUI.Controls.Backgrounds;

namespace ZapretUI.Controls;

public sealed class HomeBackgroundHost : Grid
{
    private AnimatedBackgroundBase? _background;

    public string BackgroundId { get; private set; } = HomeBackgroundCatalog.All[0].Id;

    public void SetBackground(string? id, double? motionSpeed = null)
    {
        BackgroundId = HomeBackgroundCatalog.Normalize(id);
        var speed = motionSpeed ?? AnimatedBackgroundBase.GlobalSpeed;
        Children.Clear();
        _background = HomeBackgroundFactory.Create(BackgroundId);
        if (_background is not null)
            _background.MotionSpeed = Math.Clamp(speed, 0.02, 1.5);

        if (_background is null)
        {
            Children.Add(new System.Windows.Controls.Border
            {
                Background = (System.Windows.Media.Brush)System.Windows.Application.Current
                    .FindResource("BgBrush"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            });
            return;
        }

        _background.HorizontalAlignment = HorizontalAlignment.Stretch;
        _background.VerticalAlignment = VerticalAlignment.Stretch;
        Children.Add(_background);
    }

    /// <summary>Меняет скорость без пересоздания фона — эффект сразу.</summary>
    public void ApplyMotionSpeed(double speed)
    {
        speed = Math.Clamp(speed, 0.02, 1.5);
        AnimatedBackgroundBase.GlobalSpeed = speed;
        if (_background is not null)
            _background.MotionSpeed = speed;
    }
}
