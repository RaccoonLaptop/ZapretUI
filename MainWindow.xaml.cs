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
    private Button? _activeNav;
    private HomePage? _homePage;
    private ToolsWindow? _toolsWindow;
    private bool _isShuttingDown;

    public MainWindow()
    {
        _settings = AppSettings.Load();
        InitializeComponent();
        AppIcon.ApplyTo(this);

        InitAppBackground();
        _paths = new ZapretPaths(_settings.ZapretRoot);

        if (!_paths.IsValid)
        {
            MessageBox.Show(
                "Компоненты zapret не установлены. Перезапустите программу.",
                "Zapret UI",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        _runner = new ProcessRunner();
        _runner.SetZapretRoot(_paths.Root);
        _runner.OutputReceived += line => ConsoleLog.Instance.Write(line);
        _strategy = new StrategyService(_paths, _runner);

        _tray = new TrayIconService(this);
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        VersionText.Text = $"v{AppSelfUpdateService.GetLocalVersion()} · Flowseal {_paths.GetLocalVersion()}";

        BuildNavigation();
        NavigateHome();

        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();
        RefreshStatus();

        Closing += OnClosing;
        StateChanged += OnStateChanged;
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

            if (!_settings.SecuritySetupCompleted && !_settings.SecuritySetupSkipped)
                ShowSecuritySetup();
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка при запуске: {ex.Message}");
        }
    }

    private void ShowSecuritySetup()
    {
        var setup = new SetupWindow(new SecuritySetupService(_paths), _settings) { Owner = this };
        setup.ShowDialog();
    }

    public void RunSecuritySetup() => ShowSecuritySetup();

    public void OpenToolsWindow()
    {
        if (_toolsWindow is { IsLoaded: true })
        {
            _toolsWindow.Activate();
            _toolsWindow.Focus();
            return;
        }

        _toolsWindow = new ToolsWindow(_paths, _runner)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        _toolsWindow.Closed += (_, _) =>
        {
            _toolsWindow = null;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Focus();
        };
        _toolsWindow.Show();
    }

    public void ShutdownApplication()
    {
        ExecuteShutdown();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isShuttingDown) return;

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
                ConsoleLog.Instance.Write($"Ошибка при остановке winws перед выходом: {ex.Message}");
            }
        }

        _toolsWindow?.Close();
        _statusTimer.Stop();
        _tray.Dispose();
        Application.Current.Shutdown();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_isShuttingDown) return;
        if (WindowState == WindowState.Minimized && _strategy.IsRunning())
            HideToTray();
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

    private void InitAppBackground()
    {
        AnimatedBackgroundBase.GlobalSpeed = BackgroundMotion.DefaultSpeed;
        AppBackgroundHost.SetBackground(_settings.HomeBackground, BackgroundMotion.DefaultSpeed);
        UpdateBgSwitchLabel();

        BgSwitchBtn.MouseEnter += (_, _) => BgSwitchBtn.Opacity = 0.72;
        BgSwitchBtn.MouseLeave += (_, _) => BgSwitchBtn.Opacity = 0.38;
    }

    private void UpdateBgSwitchLabel()
    {
        var entry = HomeBackgroundCatalog.Get(_settings.HomeBackground);
        BgSwitchBtn.Content = $"✦  {entry.Label}";
    }

    private void BgSwitchBtn_Click(object sender, RoutedEventArgs e)
    {
        var next = HomeBackgroundCatalog.Next(_settings.HomeBackground);
        _settings.HomeBackground = next.Id;
        _settings.Save();
        AppBackgroundHost.SetBackground(next.Id, BackgroundMotion.DefaultSpeed);
        UpdateBgSwitchLabel();
        ConsoleLog.Instance.Write($"Фон: {next.Label}");
    }

    private void BuildNavigation()
    {
        AddNav("Главная", NavigateHome);
        AddNav("Стратегии", () => Navigate(new StrategiesPage(_paths, _strategy, _settings)));
        AddNav("Сервис", () => Navigate(new ServicePage(_paths, _strategy)));
        AddNav("Консоль", OpenToolsWindow);
    }

    private void NavigateHome()
    {
        _homePage = new HomePage(_paths, _strategy, _settings);
        Navigate(_homePage);
    }

    private void AddNav(string text, Action action)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)FindResource("NavButton"),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 2, 0, 2)
        };
        btn.Click += (_, _) =>
        {
            SetActiveNav(btn);
            action();
        };
        NavPanel.Children.Add(btn);
        if (_activeNav is null)
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

    private void Navigate(UserControl page)
    {
        PageHost.Content = page;
        var onHome = page is HomePage;
        BgControlsPanel.Visibility = onHome ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshStatus()
    {
        var running = _strategy.IsRunning();
        StatusDot.Fill = running
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");
        StatusText.Text = running ? "Работает" : "Остановлен";
        if (running)
        {
            var title = _strategy.GetRunningStrategyTitle();
            if (!string.IsNullOrEmpty(title))
                StatusText.Text = $"Работает — {title}";
        }
        _homePage?.RefreshToggleUi();
    }
}
