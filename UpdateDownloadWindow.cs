using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZapretUI;

public sealed class UpdateDownloadWindow : Window
{
    private readonly TextBlock _status;

    public UpdateDownloadWindow(string title, string message)
    {
        Title = title;
        Width = 460;
        Height = 170;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("BgBrush");
        ShowInTaskbar = false;

        var root = new StackPanel { Margin = new Thickness(20) };
        _status = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        var bar = new ProgressBar
        {
            IsIndeterminate = true,
            Height = 10,
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            Background = (Brush)Application.Current.FindResource("SurfaceLightBrush")
        };
        root.Children.Add(_status);
        root.Children.Add(bar);
        Content = root;
    }

    public void SetStatus(string text) => _status.Text = text;
}
