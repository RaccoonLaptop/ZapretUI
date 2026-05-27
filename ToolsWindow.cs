using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI;

public sealed class ToolsWindow : Window
{
    private readonly ProcessRunner _runner;
    private readonly RichTextBox _output;

    public ToolsWindow(ZapretPaths paths, ProcessRunner runner)
    {
        _runner = runner;

        Title = "Zapret UI — Консоль (Niko)";
        Width = 720;
        Height = 520;
        MinWidth = 500;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
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

        _output = new RichTextBox
        {
            IsReadOnly = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderThickness = new Thickness(0),
            Document = new FlowDocument { PagePadding = new Thickness(4) }
        };

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(toolbar, Dock.Top);
        toolbar.Children.Add(MakeButton("Диагностика", async () => await RunDiagnosticsAsync()));
        toolbar.Children.Add(MakeButton("Тест стратегий", async () => await RunTestsAsync()));
        toolbar.Children.Add(MakeButton("Очистить", () =>
        {
            _output.Document.Blocks.Clear();
            AppendLine("Консоль очищена.");
            return Task.CompletedTask;
        }));
        root.Children.Add(toolbar);
        AppendLine("Консоль Zapret UI — здесь отображается вывод.");

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

    private void AppendLine(string line) => ColoredLog.AppendParagraph(_output, line);

    private void OnLineAdded(string line)
    {
        Dispatcher.Invoke(() =>
        {
            if (line == "__CLEAR__")
            {
                _output.Document.Blocks.Clear();
                return;
            }
            AppendLine(line);
        });
    }

    private async Task RunDiagnosticsAsync()
    {
        AppendLine("--- Диагностика ---");
        try
        {
            var result = await _runner.RunBridgeAsync("RunDiagnostics");
            if (!string.IsNullOrWhiteSpace(result))
                AppendLine(result);
            AppendLine("--- Готово ---");
        }
        catch (Exception ex)
        {
            AppendLine($"Ошибка: {ex.Message}");
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
            AppendLine($"Ошибка: {ex.Message}");
            UiHelpers.ShowError(ex.Message);
        }
    }
}
