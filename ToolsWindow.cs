using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI;

public sealed class ToolsWindow : Window
{
    private readonly ProcessRunner _runner;
    private readonly TextBox _output;

    public ToolsWindow(ZapretPaths paths, ProcessRunner runner)
    {
        _runner = runner;

        Title = "Zapret UI — Инструменты";
        Width = 720;
        Height = 520;
        MinWidth = 500;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("BgBrush");
        TrySetIcon();

        var root = new DockPanel { Margin = new Thickness(16) };

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(header, Dock.Top);
        header.Children.Add(new TextBlock
        {
            Text = "Консоль и диагностика",
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = "Вывод операций, проверка системы и тест стратегий",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        root.Children.Add(header);

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(toolbar, Dock.Top);
        toolbar.Children.Add(MakeButton("Диагностика", async () => await RunDiagnosticsAsync()));
        toolbar.Children.Add(MakeButton("Тест стратегий", async () => await RunTestsAsync()));
        toolbar.Children.Add(MakeButton("Очистить", () =>
        {
            _output.Clear();
            _output.Text = "Консоль очищена.\r\n";
            return Task.CompletedTask;
        }));
        root.Children.Add(toolbar);

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
            Text = "Консоль Zapret UI — здесь отображается вывод.\r\n"
        };

        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = _output
        };
        root.Children.Add(border);

        Content = root;

        ConsoleLog.Instance.LineAdded += OnLineAdded;
        Closed += (_, _) => ConsoleLog.Instance.LineAdded -= OnLineAdded;
    }

    private void TrySetIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
                Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
        catch { /* ignore */ }
    }

    private Button MakeButton(string text, Func<Task> action)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 8)
        };
        btn.Click += async (_, _) => await action();
        return btn;
    }

    private void OnLineAdded(string line)
    {
        Dispatcher.Invoke(() =>
        {
            if (line == "__CLEAR__")
            {
                _output.Clear();
                return;
            }
            _output.AppendText(line + Environment.NewLine);
            _output.ScrollToEnd();
        });
    }

    private async Task RunDiagnosticsAsync()
    {
        ConsoleLog.Instance.Write("--- Диагностика ---");
        try
        {
            await _runner.RunBridgeAsync("RunDiagnostics");
            ConsoleLog.Instance.Write("--- Готово ---");
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка: {ex.Message}");
        }
    }

    private async Task RunTestsAsync()
    {
        try
        {
            await _runner.RunInteractiveTestAsync();
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка: {ex.Message}");
            UiHelpers.ShowError(ex.Message);
        }
    }
}
