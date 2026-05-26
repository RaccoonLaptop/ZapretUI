using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class ConsolePage : UserControl
{
    private readonly TextBox _output;

    public ConsolePage()
    {
        _output = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderThickness = new Thickness(0),
            Text = "Консоль Zapret UI — здесь отображается вывод тестов, диагностики и операций.\r\n"
        };

        ConsoleLog.Instance.LineAdded += OnLineAdded;

        var root = new DockPanel();
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        DockPanel.SetDock(header, Dock.Top);
        header.Children.Add(new TextBlock { Text = "Консоль", FontSize = 28, FontWeight = FontWeights.Bold });
        header.Children.Add(new TextBlock
        {
            Text = "Вывод диагностики, тестирования и сервисных операций в реальном времени",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
        });

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(toolbar, Dock.Top);
        var clearBtn = new Button
        {
            Content = "Очистить",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        clearBtn.Click += (_, _) =>
        {
            _output.Clear();
            _output.Text = "Консоль очищена.\r\n";
        };
        toolbar.Children.Add(clearBtn);
        root.Children.Add(header);
        root.Children.Add(toolbar);

        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Child = _output
        };
        root.Children.Add(border);
        Content = root;
    }

    private void OnLineAdded(string line)
    {
        if (line == "__CLEAR__")
        {
            Dispatcher.Invoke(() => _output.Clear());
            return;
        }
        Dispatcher.Invoke(() =>
        {
            _output.AppendText(line + Environment.NewLine);
            _output.ScrollToEnd();
        });
    }
}
