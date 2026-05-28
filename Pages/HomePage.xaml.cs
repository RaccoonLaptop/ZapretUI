using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private TextBlock _actionStatus = null!;
    private readonly DispatcherTimer _statusTimer;
    private bool _isStarting;
    private bool _suppressComboChange;
    private CancellationTokenSource? _startCts;

    public bool IsBypassBusy => _isStarting;

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

    public string? GetSelectedStrategy() =>
        _strategyCombo.SelectedItem as string ?? _settings.LastStrategy;

    public async Task SwitchStrategyAsync(string strategy)
    {
        if (!_strategyCombo.Items.Contains(strategy))
            return;

        _suppressComboChange = true;
        _strategyCombo.SelectedItem = strategy;
        _suppressComboChange = false;

        _settings.LastStrategy = strategy;
        _settings.Save();

        if (!_strategy.IsRunning() && !_isStarting)
            return;

        if (_isStarting)
        {
            _startCts?.Cancel();
            while (_isStarting)
                await Task.Delay(50);
        }

        await _strategy.StopStrategyAsync();
        _isStarting = false;
        _actionStatus.Visibility = Visibility.Collapsed;

        _startCts?.Dispose();
        _startCts = new CancellationTokenSource();

        try
        {
            _isStarting = true;
            _actionStatus.Visibility = Visibility.Visible;
            _actionStatus.Text = Loc.T("home.prep");
            RefreshToggleUi();
            ConsoleLog.Instance.Write(Loc.F("home.log_start", strategy));
            _actionStatus.Text = Loc.T("home.wait_winws");
            await _strategy.StartStrategyAsync(strategy, _startCts.Token);
            ConsoleLog.Instance.Write(Loc.T("home.log_started"));
            _actionStatus.Text = Loc.T("home.done");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"{Loc.T("common.error_prefix")} {ex.Message}");
            UiHelpers.ShowError(ex.Message);
            _actionStatus.Text = Loc.T("home.start_error");
        }
        finally
        {
            _startCts?.Dispose();
            _startCts = null;
            _isStarting = false;
            _actionStatus.Visibility = Visibility.Collapsed;
            RefreshToggleUi();
        }
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
            Text = Loc.T("home.title"),
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });
        center.Children.Add(new TextBlock
        {
            Text = Loc.T("home.subtitle"),
            FontSize = 15,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 28)
        });

        _strategyCombo = new ComboBox
        {
            MinWidth = 400,
            FontSize = 15,
            Margin = new Thickness(0, 0, 0, 24)
        };
        foreach (var s in _paths.GetStrategyFiles())
            _strategyCombo.Items.Add(s);
        SelectDefaultStrategy();
        _strategyCombo.SelectionChanged += async (_, _) => await OnStrategySelectionChangedAsync();
        center.Children.Add(_strategyCombo);

        _toggleBtn = new Button
        {
            Content = Loc.T("home.start"),
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
        _suppressComboChange = true;
        var preferred = new[] { "general.bat", "general (SIMPLE FAKE).bat" };
        foreach (var name in preferred)
        {
            if (_strategyCombo.Items.Contains(name))
            {
                _strategyCombo.SelectedItem = name;
                _suppressComboChange = false;
                return;
            }
        }
        if (!string.IsNullOrEmpty(_settings.LastStrategy) && _strategyCombo.Items.Contains(_settings.LastStrategy))
            _strategyCombo.SelectedItem = _settings.LastStrategy;
        else if (_strategyCombo.Items.Count > 0)
            _strategyCombo.SelectedIndex = 0;
        _suppressComboChange = false;
    }

    private async Task OnStrategySelectionChangedAsync()
    {
        if (_suppressComboChange) return;
        if (_strategyCombo.SelectedItem is not string strategy) return;
        if (_isStarting) return;

        if (_strategy.IsRunning())
            await SwitchStrategyAsync(strategy);
        else
        {
            _settings.LastStrategy = strategy;
            _settings.Save();
        }
    }

    private void CompleteStartingUi()
    {
        if (!_isStarting) return;
        _isStarting = false;
        _actionStatus.Visibility = Visibility.Collapsed;
    }

    public void RefreshToggleUi()
    {
        var running = _strategy.IsRunning();
        if (_isStarting && running)
            CompleteStartingUi();

        _toggleBtn.Content = _isStarting && !running
            ? Loc.T("home.starting")
            : (running || _isStarting ? Loc.T("home.stop") : Loc.T("home.start"));
        _toggleBtn.IsEnabled = true;
        _strategyCombo.IsEnabled = !_isStarting;
    }

    public Task ToggleBypassAsync() => ToggleAsync();

    private async Task ToggleAsync()
    {
        if (_strategy.IsRunning() || _isStarting)
        {
            _startCts?.Cancel();
            await _strategy.StopStrategyAsync();
            _isStarting = false;
            _actionStatus.Visibility = Visibility.Collapsed;
            ConsoleLog.Instance.Write(Loc.T("home.log_stopped"));
            RefreshToggleUi();
            return;
        }

        if (_strategyCombo.SelectedItem is not string strategy)
        {
            UiHelpers.ShowError(Loc.T("home.select_strategy"));
            return;
        }

        _startCts?.Cancel();
        _startCts?.Dispose();
        _startCts = new CancellationTokenSource();

        try
        {
            _isStarting = true;
            _actionStatus.Visibility = Visibility.Visible;
            _actionStatus.Text = Loc.T("home.prep");
            RefreshToggleUi();

            _settings.LastStrategy = strategy;
            _settings.Save();
            ConsoleLog.Instance.Write(Loc.F("home.log_start", strategy));

            _actionStatus.Text = Loc.T("home.wait_winws");
            await _strategy.StartStrategyAsync(strategy, _startCts.Token);

            ConsoleLog.Instance.Write(Loc.T("home.log_started"));
            _actionStatus.Text = Loc.T("home.done");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"{Loc.T("common.error_prefix")} {ex.Message}");
            UiHelpers.ShowError(ex.Message);
            _actionStatus.Text = Loc.T("home.start_error");
        }
        finally
        {
            _startCts?.Dispose();
            _startCts = null;
            _isStarting = false;
            _actionStatus.Visibility = Visibility.Collapsed;
            RefreshToggleUi();
        }
    }
}
