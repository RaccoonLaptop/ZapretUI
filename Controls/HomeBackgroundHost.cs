using System.Windows;
using System.Windows.Controls;
using ZapretUI.Controls.Backgrounds;

namespace ZapretUI.Controls;

public sealed class HomeBackgroundHost : Grid
{
    private AnimatedBackgroundBase? _background;

    public string BackgroundId { get; private set; } = HomeBackgroundCatalog.All[0].Id;

    public AnimatedBackgroundBase? CurrentBackground => _background;

    public void SetBackground(string? id, double? motionSpeed = null)
    {
        BackgroundId = HomeBackgroundCatalog.Normalize(id);
        var speed = motionSpeed ?? AnimatedBackgroundBase.GlobalSpeed;
        Children.Clear();
        _background = HomeBackgroundFactory.Create(BackgroundId);

        if (_background is null)
        {
            Children.Add(new Border
            {
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("BgBrush"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            });
            return;
        }

        _background.SetMotionSpeed(speed);
        _background.HorizontalAlignment = HorizontalAlignment.Stretch;
        _background.VerticalAlignment = VerticalAlignment.Stretch;
        Children.Add(_background);
    }

    public void ApplyMotionSpeed(double speed)
    {
        speed = Math.Clamp(speed, 0.05, 5.0);
        AnimatedBackgroundBase.GlobalSpeed = speed;
        _background?.SetMotionSpeed(speed);
    }
}
