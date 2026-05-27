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
    private ComboBox _strategyCombo = null!;
    private Button _toggleBtn = null!;
    private Ellipse _statusIndicator = null!;
    private TextBlock _statusLabel = null!;
    private TextBlock _actionStatus = null!;
    private readonly DispatcherTimer _statusTimer;
    private bool _isStarting;

    public HomePage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += (_, _) => RefreshToggleUi();
        BuildUi();
        _statusTimer.Start();
        RefreshToggleUi();
    }

    private void BuildUi()
    {
        var center = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 460
        };

        center.Children.Add(CreateHeader());

        center.Children.Add(new TextBlock
        {
            Text = "Обход блокировок",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });
        center.Children.Add(new TextBlock
        {
            Text = "Выберите стратегию и нажмите кнопку",
            FontSize = 15,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 28)
        });

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
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
            FontSize = 17,
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
            Margin = new Thickness(0, 0, 0, 24)
        };
        foreach (var s in _paths.GetStrategyFiles())
            _strategyCombo.Items.Add(s);
        SelectDefaultStrategy();
        center.Children.Add(_strategyCombo);

        _toggleBtn = new Button
        {
            Content = "▶   ЗАПУСТИТЬ",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            FontSize = 18,
            Padding = new Thickness(56, 16, 56, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 280
        };
        _toggleBtn.Click += async (_, _) => await ToggleAsync();
        center.Children.Add(_toggleBtn);

        _actionStatus = new TextBlock
        {
            Text = "",
            FontSize = 13,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0),
            Visibility = Visibility.Collapsed
        };
        center.Children.Add(_actionStatus);

        Content = center;
    }

    private static UIElement CreateHeader() => new TextBlock
    {
        Text = "✦",
        FontSize = 42,
        HorizontalAlignment = HorizontalAlignment.Center,
        Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
        Opacity = 0.85,
        Margin = new Thickness(0, 0, 0, 8)
    };

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

    public void RefreshToggleUi()
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

        _toggleBtn.Content = _isStarting
            ? "⏳   ЗАПУСКАЕТСЯ..."
            : (running ? "⏹   ОСТАНОВИТЬ" : "▶   ЗАПУСТИТЬ");
        _toggleBtn.IsEnabled = !_isStarting;
        _strategyCombo.IsEnabled = !running && !_isStarting;
    }

    private async Task ToggleAsync()
    {
        if (_strategy.IsRunning())
        {
            await _strategy.StopStrategyAsync();
            ConsoleLog.Instance.Write("Остановлено");
            RefreshToggleUi();
            return;
        }

        if (_strategyCombo.SelectedItem is not string strategy)
        {
            UiHelpers.ShowError("Выберите стратегию");
            return;
        }

        try
        {
            _isStarting = true;
            _actionStatus.Visibility = Visibility.Visible;
            _actionStatus.Text = "Подготовка запуска...";
            RefreshToggleUi();

            _settings.LastStrategy = strategy;
            _settings.Save();
            ConsoleLog.Instance.Write($"Запуск: {strategy}");

            _actionStatus.Text = "Запускаем winws, подождите...";
            await _strategy.StartStrategyAsync(strategy);

            ConsoleLog.Instance.Write("Запущено");
            _actionStatus.Text = "Запуск завершен";
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка: {ex.Message}");
            UiHelpers.ShowError(ex.Message);
            _actionStatus.Text = "Ошибка запуска";
        }
        finally
        {
            _isStarting = false;
            if (_strategy.IsRunning())
            {
                _ = Task.Delay(1200).ContinueWith(_ =>
                    Dispatcher.Invoke(() => _actionStatus.Visibility = Visibility.Collapsed));
            }
        }
        RefreshToggleUi();
    }
}
