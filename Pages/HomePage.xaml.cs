using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    private ToggleButton _autostartToggle = null!;
    private Button _toggleBtn = null!;
    private TextBlock _presetHint = null!;
    private TextBlock _actionStatus = null!;
    private readonly DispatcherTimer _statusTimer;
    private bool _isStarting;
    private bool _suppressComboChange;
    private CancellationTokenSource? _startCts;
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    public bool IsBypassBusy => _isStarting;

    public HomePage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += (_, _) => RefreshToggleUi();
        Unloaded += (_, _) => _statusTimer.Stop();
        BuildUi();
        _statusTimer.Start();
        RefreshToggleUi();
    }

    public string? GetSelectedStrategy() => GetSelectedFileName();

    public async Task SwitchStrategyAsync(string strategy)
    {
        await _operationGate.WaitAsync();
        _suppressComboChange = true;
        try
        {
            if (!TrySelectStrategy(strategy))
                return;

            _settings.LastStrategy = strategy;
            _settings.Save();
            UpdatePresetHint();

            if (!_strategy.IsRunning() && !_isStarting)
                return;

            _startCts?.Cancel();
            _startCts?.Dispose();
            _startCts = new CancellationTokenSource();
            var ct = _startCts.Token;

            try
            {
                _isStarting = true;
                _actionStatus.Visibility = Visibility.Visible;
                _actionStatus.Text = Loc.T("home.prep");
                RefreshToggleUi();

                await _strategy.StopStrategyAsync(ct);
                _actionStatus.Text = Loc.T("home.wait_winws");
                ConsoleLog.Instance.Write(Loc.F("home.log_start", strategy));
                await _strategy.StartStrategyAsync(strategy, ct, quickSwitch: true);
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
        finally
        {
            _suppressComboChange = false;
            _operationGate.Release();
        }
    }

    private void BuildUi()
    {
        var center = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 480
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
            Margin = new Thickness(0, 0, 0, 24)
        });

        _strategyCombo = new ComboBox
        {
            MinWidth = 400,
            FontSize = 15,
            Margin = new Thickness(0, 0, 0, 0),
            DisplayMemberPath = nameof(StrategyItem.DisplayName),
            SelectedValuePath = nameof(StrategyItem.FileName)
        };
        foreach (var item in StrategyDisplayHelper.LoadItems(_paths.Root, _paths.GetStrategyFiles()))
            _strategyCombo.Items.Add(item);
        _strategyCombo.SelectionChanged += async (_, _) => await OnStrategySelectionChangedAsync();
        center.Children.Add(_strategyCombo);

        _presetHint = new TextBlock
        {
            FontSize = 13,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 10, 0, 20)
        };
        SelectDefaultStrategy();
        center.Children.Add(_presetHint);

        center.Children.Add(CreateAutostartRow());

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

    private UIElement CreateAutostartRow()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 24)
        };

        row.Children.Add(new TextBlock
        {
            Text = Loc.T("home.autostart"),
            FontSize = 15,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });

        _autostartToggle = new ToggleButton
        {
            Style = (Style)Application.Current.FindResource("SwitchToggle"),
            IsChecked = _settings.StartUiOnLogin && AppStartupService.IsEnabled(),
            VerticalAlignment = VerticalAlignment.Center
        };
        _autostartToggle.Checked += (_, _) => SetAutostartEnabled(true);
        _autostartToggle.Unchecked += (_, _) => SetAutostartEnabled(false);
        row.Children.Add(_autostartToggle);

        return row;
    }

    private void SetAutostartEnabled(bool enabled)
    {
        if (enabled)
        {
            var strategy = GetSelectedFileName();
            if (!string.IsNullOrEmpty(strategy))
                _settings.LastStrategy = strategy;
            AppStartupService.Enable();
            _settings.StartUiOnLogin = true;
        }
        else
        {
            AppStartupService.Disable();
            _settings.StartUiOnLogin = false;
        }
        _settings.Save();
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
        try
        {
            if (TrySelectStrategy(_settings.LastStrategy))
                return;

            var running = _strategy.GetRunningStrategyTitle();
            if (!string.IsNullOrEmpty(running))
            {
                var runningBat = running.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)
                    ? running
                    : running + ".bat";
                if (TrySelectStrategy(runningBat))
                    return;
            }

            foreach (var name in new[] { "general.bat", "general (SIMPLE FAKE).bat" })
            {
                if (TrySelectStrategy(name))
                    return;
            }

            if (_strategyCombo.Items.Count > 0)
                _strategyCombo.SelectedIndex = 0;
        }
        finally
        {
            _suppressComboChange = false;
            UpdatePresetHint();
        }
    }

    private bool TrySelectStrategy(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        foreach (StrategyItem item in _strategyCombo.Items)
        {
            if (!item.FileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (ReferenceEquals(_strategyCombo.SelectedItem, item))
                return true;

            if (_suppressComboChange)
            {
                _strategyCombo.SelectedItem = item;
                return true;
            }

            _suppressComboChange = true;
            try
            {
                _strategyCombo.SelectedItem = item;
            }
            finally
            {
                _suppressComboChange = false;
            }

            return true;
        }

        return false;
    }

    private StrategyItem? GetSelectedStrategyItem() =>
        _strategyCombo.SelectedItem as StrategyItem;

    private string? GetSelectedFileName() =>
        GetSelectedStrategyItem()?.FileName ?? _settings.LastStrategy;

    private void UpdatePresetHint()
    {
        if (_presetHint is null) return;
        _presetHint.Text = StrategyDisplayHelper.GetHintText(GetSelectedStrategyItem());
    }

    private async Task OnStrategySelectionChangedAsync()
    {
        if (_suppressComboChange) return;

        try
        {
            if (GetSelectedFileName() is not { } strategy) return;
            if (_isStarting) return;

            UpdatePresetHint();

            if (_strategy.IsRunning())
                await SwitchStrategyAsync(strategy);
            else
            {
                _settings.LastStrategy = strategy;
                _settings.Save();
            }
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"{Loc.T("common.error_prefix")} {ex.Message}");
            UiHelpers.ShowError(ex.Message);
        }
    }

    public void RefreshToggleUi()
    {
        if (_toggleBtn is null || _strategyCombo is null)
            return;

        var running = _strategy.IsRunning();

        _toggleBtn.Content = _isStarting && !running
            ? Loc.T("home.starting")
            : (running || _isStarting ? Loc.T("home.stop") : Loc.T("home.start"));
        _toggleBtn.IsEnabled = !_isStarting;
        _strategyCombo.IsEnabled = !_isStarting;

        _toggleBtn.Style = (Style)Application.Current.FindResource(
            running && !_isStarting ? "SuccessButton" : "PrimaryButton");
    }

    public Task ToggleBypassAsync() => ToggleAsync();

    private async Task ToggleAsync()
    {
        await _operationGate.WaitAsync();
        try
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

            if (GetSelectedFileName() is not { } strategy)
            {
                UiHelpers.ShowError(Loc.T("home.select_strategy"));
                return;
            }

            _startCts?.Cancel();
            _startCts?.Dispose();
            _startCts = new CancellationTokenSource();
            var ct = _startCts.Token;

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
                await _strategy.StartStrategyAsync(strategy, ct);

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
        finally
        {
            _operationGate.Release();
        }
    }
}
