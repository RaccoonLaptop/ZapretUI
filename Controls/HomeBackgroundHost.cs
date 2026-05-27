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
        if (_background is null) return;

        _background.HorizontalAlignment = HorizontalAlignment.Stretch;
        _background.VerticalAlignment = VerticalAlignment.Stretch;
        Children.Add(_background);
    }
}
