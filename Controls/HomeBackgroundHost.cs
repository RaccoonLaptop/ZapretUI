using System.Windows;
using System.Windows.Controls;
using ZapretUI.Controls.Backgrounds;

namespace ZapretUI.Controls;

public sealed class HomeBackgroundHost : Grid
{
    private AnimatedBackgroundBase? _background;

    public string BackgroundId { get; private set; } = HomeBackgroundCatalog.All[0].Id;

    public void SetBackground(string? id)
    {
        BackgroundId = HomeBackgroundCatalog.Normalize(id);
        Children.Clear();
        _background = HomeBackgroundFactory.Create(BackgroundId);
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
}
