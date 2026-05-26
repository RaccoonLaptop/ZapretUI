using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class HomePage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private readonly ProcessRunner _runner;
    private ComboBox _strategyCombo = null!;
    private Ellipse _statusIndicator = null!;
    private TextBlock _statusLabel = null!;
    private TextBox _logBox = null!;

    public HomePage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        _runner = new ProcessRunner();
        _runner.SetZapretRoot(paths.Root);
        _runner.OutputReceived += line => ConsoleLog.Instance.Write(line);
        BuildUi();
        ConsoleLog.Instance.LineAdded += OnLogLine;
        Unloaded += (_, _) => ConsoleLog.Instance.LineAdded -= OnLogLine;
    }

    private void BuildUi()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 32) };
        header.Children.Add(new TextBlock
        {
            Text = "Обход блокировок",
            FontSize = 32,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = "Выберите стратегию и нажмите «Запустить»",
            FontSize = 16,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 8, 0, 0)
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var center = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 480
        };

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 24)
        };
        _statusIndicator = new Ellipse
        {
            Width = 14,
            Height = 14,
            Fill = (Brush)Application.Current.FindResource("ErrorBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusLabel = new TextBlock
        {
            Text = "Остановлен",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusRow.Children.Add(_statusIndicator);
        statusRow.Children.Add(_statusLabel);
        center.Children.Add(statusRow);

        _strategyCombo = new ComboBox
        {
            MinWidth = 400,
            FontSize = 15,
            Margin = new Thickness(0, 0, 0, 20)
        };
        foreach (var s in _paths.GetStrategyFiles())
            _strategyCombo.Items.Add(s);
        SelectDefaultStrategy();
        center.Children.Add(_strategyCombo);

        var startBtn = new Button
        {
            Content = "▶   ЗАПУСТИТЬ",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            FontSize = 18,
            Padding = new Thickness(48, 16, 48, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        startBtn.Click += async (_, _) => await StartAsync();
        center.Children.Add(startBtn);

        var stopBtn = new Button
        {
            Content = "Остановить",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        stopBtn.Click += async (_, _) => await StopAsync();
        center.Children.Add(stopBtn);

        var toolsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var diagBtn = new Button
        {
            Content = "Диагностика",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        diagBtn.Click += async (_, _) => await RunDiagnosticsAsync();
        var testBtn = new Button
        {
            Content = "Тест стратегий",
            Style = (Style)Application.Current.FindResource("SecondaryButton")
        };
        testBtn.Click += async (_, _) => await RunTestsAsync();
        toolsRow.Children.Add(diagBtn);
        toolsRow.Children.Add(testBtn);
        center.Children.Add(toolsRow);

        Grid.SetRow(center, 1);
        root.Children.Add(center);

        _logBox = new TextBox
        {
            Height = 100,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8)
        };
        Grid.SetRow(_logBox, 2);
        root.Children.Add(_logBox);

        Content = root;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => RefreshStatus();
        timer.Start();
        RefreshStatus();
    }

    private void SelectDefaultStrategy()
    {
        var preferred = new[] { "general.bat", "general (SIMPLE FAKE).bat" };
        foreach (var name in preferred)
        {
            if (_strategyCombo.Items.Contains(name))
            {
                _strategyCombo.SelectedItem = name;
                return;
            }
        }
        if (!string.IsNullOrEmpty(_settings.LastStrategy) && _strategyCombo.Items.Contains(_settings.LastStrategy))
            _strategyCombo.SelectedItem = _settings.LastStrategy;
        else if (_strategyCombo.Items.Count > 0)
            _strategyCombo.SelectedIndex = 0;
    }

    private void RefreshStatus()
    {
        var running = _strategy.IsRunning();
        _statusIndicator.Fill = running
            ? (Brush)Application.Current.FindResource("SuccessBrush")
            : (Brush)Application.Current.FindResource("ErrorBrush");
        _statusLabel.Text = running ? "Работает" : "Остановлен";
        if (running)
        {
            var title = _strategy.GetRunningStrategyTitle();
            if (!string.IsNullOrEmpty(title))
                _statusLabel.Text = $"Работает — {title}";
        }
    }

    private void OnLogLine(string line)
    {
        Dispatcher.Invoke(() =>
        {
            if (line == "__CLEAR__")
            {
                _logBox.Clear();
                return;
            }
            _logBox.AppendText(line + Environment.NewLine);
            _logBox.ScrollToEnd();
        });
    }

    private async Task StartAsync()
    {
        if (_strategyCombo.SelectedItem is not string strategy)
        {
            UiHelpers.ShowError("Выберите стратегию");
            return;
        }
        try
        {
            _settings.LastStrategy = strategy;
            _settings.Save();
            ConsoleLog.Instance.Write($"Запуск: {strategy}");
            await _strategy.StartStrategyAsync(strategy);
            ConsoleLog.Instance.Write("Запущено");
            RefreshStatus();
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка: {ex.Message}");
            UiHelpers.ShowError(ex.Message);
        }
    }

    private async Task StopAsync()
    {
        await _strategy.StopStrategyAsync();
        ConsoleLog.Instance.Write("Остановлено");
        RefreshStatus();
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
