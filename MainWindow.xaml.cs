using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ZapretUI.Helpers;
using ZapretUI.Pages;
using ZapretUI.Services;

namespace ZapretUI;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private ZapretPaths _paths = null!;
    private StrategyService _strategy = null!;
    private ProcessRunner _runner = null!;
    private readonly DispatcherTimer _statusTimer = null!;
    private readonly TrayIconService _tray = null!;
    private Button? _activeNav;
    private bool _isShuttingDown;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _paths = new ZapretPaths(_settings.ZapretRoot);

        if (!_paths.IsValid)
        {
            UiHelpers.ShowError("Компоненты zapret не установлены. Перезапустите программу — загрузка начнётся автоматически.");
            Application.Current.Shutdown();
            return;
        }

        InitServices();

        _tray = new TrayIconService(this);
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };

        UiVersionText.Text = $"Zapret UI v{AppSelfUpdateService.GetLocalVersion()}";
        VersionText.Text = $"Flowseal {_paths.GetLocalVersion()}";
        RootPathText.Text = _paths.Root;

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
            if (_settings.AutoUpdateApp)
            {
                ConsoleLog.Instance.Write("Проверка обновлений Zapret UI...");
                var updater = new AppSelfUpdateService(_settings, _paths.Root);
                var result = await updater.CheckAndInstallIfNeededAsync(autoInstall: true);

                if (result.RequiresRestart)
                {
                    ConsoleLog.Instance.Write(result.Message);
                    ShutdownApplication();
                    return;
                }

                if (!result.Success && result.Message is not null &&
                    !result.Message.Contains("не требуется", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleLog.Instance.Write($"Обновление Zapret UI: {result.Message}");
                }
            }

            if (!_settings.SecuritySetupCompleted && !_settings.SecuritySetupSkipped)
                ShowSecuritySetup();

            await VerifySecurityOnStartupAsync();
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка при запуске: {ex.Message}");
        }
    }

    private async Task VerifySecurityOnStartupAsync(bool promptIfIssues = true)
    {
        var security = new SecuritySetupService(_paths);
        var status = await security.CheckStatusAsync();

        if (status.IsFullyConfigured)
        {
            ConsoleLog.Instance.Write("Безопасность: исключения и брандмауэр в порядке");
            _settings.SecuritySetupCompleted = true;
            _settings.Save();
            return;
        }

        if (!status.CheckSucceeded)
        {
            ConsoleLog.Instance.Write($"Проверка безопасности: {status.Summary}");
            return;
        }

        ConsoleLog.Instance.Write($"Безопасность: {status.Summary}");

        if (!promptIfIssues) return;

        var details = status.Summary;
        if (status.MissingExclusions.Count > 0)
            details += "\n\nDefender: " + string.Join(", ", status.MissingExclusions);
        if (status.MissingFirewallPrograms.Count > 0)
            details += "\n\nБрандмауэр: " + string.Join(", ", status.MissingFirewallPrograms);

        if (UiHelpers.Confirm(
                $"Обнаружены проблемы безопасности — возможны конфликты с антивирусом или брандмауэром:\n\n{details}\n\nНастроить сейчас?"))
        {
            ShowSecuritySetup();
            await VerifySecurityOnStartupAsync(promptIfIssues: false);
        }
    }

    private void ShowSecuritySetup()
    {
        var setup = new SetupWindow(new SecuritySetupService(_paths), _settings)
        {
            Owner = this
        };
        setup.ShowDialog();
    }

    public void RunSecuritySetup()
    {
        ShowSecuritySetup();
    }

    private void InitServices()
    {
        _runner = new ProcessRunner();
        _runner.SetZapretRoot(_paths.Root);
        _runner.OutputReceived += line => ConsoleLog.Instance.Write(line);
        _strategy = new StrategyService(_paths, _runner);
    }

    public void ShutdownApplication()
    {
        _isShuttingDown = true;
        _statusTimer.Stop();
        _tray.Dispose();
        Application.Current.Shutdown();
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

        _isShuttingDown = true;
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

    private void BuildNavigation()
    {
        AddNav("🏠  Главная", NavigateHome);
        AddNav("⚡  Стратегии", () => Navigate(new StrategiesPage(_paths, _strategy, _settings)));
        AddNav("📋  Списки", () => Navigate(new ListsPage(_paths)));
        AddNav("🔧  Сервис", () => Navigate(new ServicePage(_paths, _strategy)));
        AddNav("📖  Гайд", () => Navigate(new GuidePage(_paths)));
        AddNav("💻  Консоль", () => Navigate(new ConsolePage()));

        var folderBtn = new Button
        {
            Content = "📁  Папка zapret",
            Style = (Style)FindResource("NavButton"),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 12, 0, 2)
        };
        folderBtn.Click += (_, _) => ChangeZapretFolder();
        NavPanel.Children.Add(folderBtn);
    }

    private void ChangeZapretFolder()
    {
        var picked = FolderPicker.PickFolder("Папка с service.bat и bin\\", _paths.Root);
        if (picked is null) return;

        if (!ZapretPaths.IsValidZapretRoot(picked))
        {
            UiHelpers.ShowError("В этой папке нет service.bat или bin\\. Выберите корень zapret.");
            return;
        }

        _settings.ZapretRoot = picked;
        _settings.Save();
        _paths = new ZapretPaths(picked);
        InitServices();
        RootPathText.Text = _paths.Root;
        UiVersionText.Text = $"Zapret UI v{AppSelfUpdateService.GetLocalVersion()}";
        VersionText.Text = $"Flowseal {_paths.GetLocalVersion()}";
        NavigateHome();
        ConsoleLog.Instance.Write($"Папка zapret: {picked}");
    }

    private void NavigateHome() => Navigate(new HomePage(_paths, _strategy, _settings));

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

    private void Navigate(UserControl page) => PageHost.Content = page;

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
                StatusText.Text = $"Работает: {title}";
        }
    }
}
