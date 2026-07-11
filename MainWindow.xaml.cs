using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZapretUI.Controls.Backgrounds;
using ZapretUI.Helpers;
using ZapretUI.Pages;
using ZapretUI.Services;

namespace ZapretUI;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly ProcessRunner _runner;
    private readonly DispatcherTimer _statusTimer;
    private readonly TrayIconService _tray;
    private readonly PowerResumeService _powerResume;
    private readonly bool _startInTray;
    private Button? _activeNav;
    private string _activeSection = "home";
    private HomePage? _homePage;
    private TestStrategiesPage? _testStrategiesPage;
    private bool _isShuttingDown;

    public MainWindow(bool startInTray = false)
    {
        _startInTray = startInTray;
        _settings = AppSettings.Load();
        InitializeComponent();
        RestoreWindowBounds();
        ApplyShellLocalization();
        AppIcon.ApplyTo(this);

        InitAppBackground();
        _paths = new ZapretPaths(_settings.ZapretRoot);

        if (!_paths.IsValid)
        {
            MessageBox.Show(
                Loc.T("app.zapret_missing"),
                Loc.T("app.title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        _runner = new ProcessRunner();
        _runner.SetZapretRoot(_paths.Root);
        _runner.OutputReceived += line => ConsoleLog.Instance.Write(line);
        _strategy = new StrategyService(_paths, _runner);

        _tray = new TrayIconService(
            this,
            ToggleBypassFromTrayAsync,
            () => StrategyDisplayHelper.LoadItems(_paths.Root, _paths.GetStrategyFiles()),
            () => _homePage?.GetSelectedStrategy() ?? _settings.LastStrategy,
            SwitchStrategyFromTrayAsync);
        _powerResume = new PowerResumeService(
            () => _strategy.IsRunning(),
            RestartBypassAfterResumeAsync);
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        VersionText.Text = $"v{AppSelfUpdateService.GetLocalVersion()} · Flowseal {_paths.GetLocalVersion()}";

        BuildNavigation();
        NavigateHome();

        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();
        RefreshStatus();

        Closing += OnClosing;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var startupUpdates = new StartupUpdateService();
            if (await startupUpdates.CheckAndPromptAsync(this, _settings, _paths))
            {
                ShutdownApplication();
                return;
            }

            if (!_settings.SecuritySetupCompleted && !_settings.SecuritySetupSkipped && !_startInTray)
                ShowSecuritySetup();

            if (_startInTray)
                HideToTray();

            if (_startInTray)
                await TryAutoStartBypassOnLoginAsync();
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write(Loc.F("startup.error", ex.Message));
        }
    }

    private void ShowSecuritySetup()
    {
        var setup = new SetupWindow(new SecuritySetupService(_paths), _settings) { Owner = this };
        setup.ShowDialog();
    }

    public void RunSecuritySetup() => ShowSecuritySetup();

    public void ApplyLanguageChange()
    {
        ApplyShellLocalization();
        UpdateBgSwitchLabel();

        NavPanel.Children.Clear();
        _activeNav = null;
        BuildNavigation();
        NavigateToSection(_activeSection);
        RefreshStatus();
    }

    public void ShutdownApplication()
    {
        ExecuteShutdown();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_isShuttingDown && WindowState != WindowState.Minimized)
            SaveWindowBounds();

        if (_isShuttingDown) return;

        if (LocalizationService.RestartPending)
        {
            ExecuteShutdown();
            return;
        }

        if (_strategy.IsRunning())
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        ExecuteShutdown();
    }

    private void ExecuteShutdown()
    {
        SaveWindowBounds();
        _testStrategiesPage?.SaveSession();
        _isShuttingDown = true;

        // Если обход был запущен, сначала останавливаем winws, затем закрываем UI.
        if (_strategy.IsRunning())
        {
            try
            {
                _strategy.StopStrategyAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ConsoleLog.Instance.Write(Loc.F("shutdown.stop_error", ex.Message));
            }
        }

        try
        {
            _testStrategiesPage?.DisposePanelAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write(Loc.F("shutdown.stop_error", ex.Message));
        }

        _statusTimer.Stop();
        _powerResume.Dispose();
        _tray.Dispose();
        Application.Current.Shutdown();
    }

    public void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    private async Task TryAutoStartBypassOnLoginAsync()
    {
        if (!_settings.StartUiOnLogin || _strategy.IsRunning())
            return;

        var strategy = _settings.LastStrategy;
        if (string.IsNullOrWhiteSpace(strategy))
            return;

        var batPath = Path.Combine(_paths.Root, strategy);
        if (!File.Exists(batPath))
            return;

        try
        {
            ConsoleLog.Instance.Write(Loc.F("startup.autostart_bypass", strategy));
            await _strategy.StartStrategyAsync(strategy);
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write(Loc.F("startup.autostart_bypass_failed", ex.Message));
        }
    }

    private async Task RestartBypassAfterResumeAsync()
    {
        if (_strategy.IsRunning())
            return;

        var strategy = _homePage?.GetSelectedStrategy() ?? _settings.LastStrategy;
        if (string.IsNullOrWhiteSpace(strategy))
            return;

        var batPath = Path.Combine(_paths.Root, strategy);
        if (!File.Exists(batPath))
            return;

        ConsoleLog.Instance.Write(Loc.T("power.resume_restarting"));
        await _strategy.StartStrategyAsync(strategy);
        ConsoleLog.Instance.Write(Loc.T("power.resume_restarted"));
    }

    private void HideToTray()
    {
        Hide();
        _tray.ShowInTray();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void RestoreWindowBounds()
    {
        if (_settings.WindowWidth is not > 0 || _settings.WindowHeight is not > 0)
            return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = Math.Max(_settings.WindowWidth.Value, MinWidth);
        Height = Math.Max(_settings.WindowHeight.Value, MinHeight);

        if (_settings.WindowLeft is double left && _settings.WindowTop is double top)
        {
            Left = left;
            Top = top;
            EnsureWindowOnScreen();
        }

        if (_settings.WindowMaximized)
            WindowState = WindowState.Maximized;
    }

    private void SaveWindowBounds()
    {
        Rect bounds;
        if (WindowState == WindowState.Maximized)
        {
            _settings.WindowMaximized = true;
            bounds = RestoreBounds;
        }
        else if (WindowState == WindowState.Normal)
        {
            _settings.WindowMaximized = false;
            bounds = new Rect(Left, Top, Width, Height);
        }
        else
        {
            return;
        }

        _settings.WindowWidth = bounds.Width;
        _settings.WindowHeight = bounds.Height;
        _settings.WindowLeft = bounds.Left;
        _settings.WindowTop = bounds.Top;
        _settings.Save();
    }

    private void EnsureWindowOnScreen()
    {
        var work = SystemParameters.WorkArea;
        if (Width > work.Width)
            Width = work.Width;
        if (Height > work.Height)
            Height = work.Height;
        if (Left + Width > work.Right)
            Left = work.Right - Width;
        if (Top + Height > work.Bottom)
            Top = work.Bottom - Height;
        if (Left < work.Left)
            Left = work.Left;
        if (Top < work.Top)
            Top = work.Top;
    }

    private void InitAppBackground()
    {
        AnimatedBackgroundBase.GlobalSpeed = BackgroundMotion.DefaultSpeed;
        AppBackgroundHost.SetBackground(_settings.HomeBackground, BackgroundMotion.DefaultSpeed);
        UpdateBgSwitchLabel();

        BgSwitchBtn.MouseEnter += (_, _) => BgSwitchBtn.Opacity = 0.72;
        BgSwitchBtn.MouseLeave += (_, _) => BgSwitchBtn.Opacity = 0.38;
    }

    private void ApplyShellLocalization()
    {
        TitleBarAuthor.Text = Loc.T("app.author_short");
        StatusHeader.Text = Loc.T("status.label");
        BgSwitchBtn.ToolTip = Loc.T("bg.switch_tooltip");
    }

    private void UpdateBgSwitchLabel()
    {
        var (_, label) = HomeBackgroundCatalog.Get(_settings.HomeBackground);
        BgSwitchBtn.Content = $"✦  {label}";
    }

    private void BgSwitchBtn_Click(object sender, RoutedEventArgs e)
    {
        var (nextId, nextLabel) = HomeBackgroundCatalog.Next(_settings.HomeBackground);
        _settings.HomeBackground = nextId;
        _settings.Save();
        AppBackgroundHost.SetBackground(nextId, BackgroundMotion.DefaultSpeed);
        UpdateBgSwitchLabel();
        ConsoleLog.Instance.Write(Loc.F("bg.log", nextLabel));
    }

    private void BuildNavigation()
    {
        AddNav(Loc.T("nav.home"), "home", NavigateHome);
        AddNav(Loc.T("nav.strategies"), "strategies", () => Navigate(new StrategiesPage(_paths, _strategy, _settings)));
        AddNav(Loc.T("nav.service"), "service", () => Navigate(new ServicePage(_paths, _strategy)));
        AddNav(Loc.T("nav.diagnostics"), "diagnostics", () => Navigate(new DiagnosticsPage(_runner)));
        AddNav(Loc.T("nav.test"), "test", NavigateTest);
    }

    private void NavigateToSection(string sectionId)
    {
        _activeSection = sectionId;
        switch (sectionId)
        {
            case "home":
                NavigateHome();
                break;
            case "strategies":
                Navigate(new StrategiesPage(_paths, _strategy, _settings));
                break;
            case "service":
                Navigate(new ServicePage(_paths, _strategy));
                break;
            case "diagnostics":
                Navigate(new DiagnosticsPage(_runner));
                break;
            case "test":
                _testStrategiesPage = null;
                NavigateTest();
                break;
        }
    }

    private void NavigateTest()
    {
        _activeSection = "test";
        _testStrategiesPage ??= new TestStrategiesPage(_paths, _strategy, _settings);
        Navigate(_testStrategiesPage);
    }

    private void NavigateHome()
    {
        _activeSection = "home";
        _homePage = new HomePage(_paths, _strategy, _settings);
        Navigate(_homePage);
    }

    private void AddNav(string text, string sectionId, Action action)
    {
        var btn = new Button
        {
            Content = text,
            Tag = sectionId,
            Style = (Style)FindResource("NavButton"),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 2, 0, 2)
        };
        btn.Click += (_, _) =>
        {
            _activeSection = sectionId;
            SetActiveNav(btn);
            action();
        };
        NavPanel.Children.Add(btn);
        if (_activeSection == sectionId)
        {
            _activeNav = btn;
            btn.Style = (Style)FindResource("NavButtonActive");
        }
    }

    private void SetActiveNav(Button btn)
    {
        if (_activeNav is not null)
            _activeNav.Style = (Style)FindResource("NavButton");
        _activeNav = btn;
        btn.Style = (Style)FindResource("NavButtonActive");
    }

    private void Navigate(UserControl page) => PageHost.Content = page;

    private void RefreshStatus()
    {
        var running = _strategy.IsRunning();
        StatusDot.Fill = running
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");

        var title = running ? _strategy.GetRunningStrategyTitle() : null;
        if (running && !string.IsNullOrEmpty(title))
        {
            StatusText.Text = Loc.T("status.running");
            StatusPresetText.Text = title;
            StatusPresetText.Visibility = Visibility.Visible;
            StatusBorder.ToolTip = Loc.F("status.running_with", title);
        }
        else
        {
            StatusText.Text = running ? Loc.T("status.running") : Loc.T("status.stopped");
            StatusPresetText.Visibility = Visibility.Collapsed;
            StatusPresetText.Text = "";
            StatusBorder.ToolTip = null;
        }

        _homePage?.RefreshToggleUi();
        _tray.UpdateState(
            running,
            _strategy.GetRunningStrategyTitle(),
            _homePage?.IsBypassBusy ?? false);
    }

    private async Task SwitchStrategyFromTrayAsync(string strategy)
    {
        if (_homePage is null)
            NavigateHome();
        await _homePage!.SwitchStrategyAsync(strategy);
    }

    private async Task ToggleBypassFromTrayAsync()
    {
        if (_homePage is null)
            NavigateHome();
        await _homePage!.ToggleBypassAsync();
    }
}
