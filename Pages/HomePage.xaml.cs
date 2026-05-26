using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class HomePage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private readonly UpdateService _updates;
    private readonly AppSelfUpdateService _appUpdater;
    private ComboBox _strategyCombo = null!;
    private TextBlock _updateStatus = null!;
    private TextBlock _appUpdateStatus = null!;
    private Ellipse _statusIndicator = null!;
    private TextBlock _statusLabel = null!;

    public HomePage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        _updates = new UpdateService(paths);
        _appUpdater = new AppSelfUpdateService(settings, paths.Root);
        BuildUi();
        _ = CheckZapretUpdatesAsync();
        _ = CheckAppUpdatesAsync(silent: true);
    }

    private void BuildUi()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel();

        root.Children.Add(new TextBlock
        {
            Text = "Главная",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        root.Children.Add(new TextBlock
        {
            Text = "Управление обходом DPI — Zapret + конфиги Flowseal",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 16)
        });

        foreach (var hint in new SystemHintsService(_paths).GetHints())
            root.Children.Add(CreateHintBanner(hint));

        var securityPanel = new StackPanel { Name = "SecurityPanel" };
        root.Children.Add(securityPanel);
        _ = LoadSecurityStatusAsync(securityPanel);

        var card = CreateCard();
        var cardContent = new StackPanel();

        cardContent.Children.Add(new TextBlock { Text = "Текущая стратегия", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });

        _strategyCombo = new ComboBox { MinWidth = 400, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var s in _paths.GetStrategyFiles())
            _strategyCombo.Items.Add(s);
        if (!string.IsNullOrEmpty(_settings.LastStrategy))
            _strategyCombo.SelectedItem = _settings.LastStrategy;
        else if (_strategyCombo.Items.Count > 0)
            _strategyCombo.SelectedIndex = 0;
        cardContent.Children.Add(_strategyCombo);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };
        var startBtn = new Button { Content = "▶  Запустить", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 12, 0) };
        startBtn.Click += async (_, _) => await StartAsync();
        var stopBtn = new Button { Content = "⏹  Остановить", Style = (Style)Application.Current.FindResource("SecondaryButton") };
        stopBtn.Click += async (_, _) => await StopAsync();
        btnRow.Children.Add(startBtn);
        btnRow.Children.Add(stopBtn);
        cardContent.Children.Add(btnRow);

        var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        _statusIndicator = new Ellipse { Width = 12, Height = 12, Fill = (Brush)Application.Current.FindResource("ErrorBrush"), VerticalAlignment = VerticalAlignment.Center };
        _statusLabel = new TextBlock { Text = " winws.exe не запущен", VerticalAlignment = VerticalAlignment.Center };
        statusRow.Children.Add(_statusIndicator);
        statusRow.Children.Add(_statusLabel);
        cardContent.Children.Add(statusRow);

        card.Child = cardContent;
        root.Children.Add(card);

        var updateCard = CreateCard();
        var updateContent = new StackPanel();
        updateContent.Children.Add(new TextBlock { Text = "Zapret UI (программа)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        _appUpdateStatus = new TextBlock
        {
            Text = $"Версия {AppSelfUpdateService.GetLocalVersion()}",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap
        };
        updateContent.Children.Add(_appUpdateStatus);
        var appUpdateBtn = new Button
        {
            Content = "Проверить и установить обновление",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        appUpdateBtn.Click += async (_, _) => await CheckAppUpdatesAsync(silent: false);
        updateContent.Children.Add(appUpdateBtn);
        updateCard.Child = updateContent;
        root.Children.Add(updateCard);

        var zapretUpdateCard = CreateCard();
        var updateContent2 = new StackPanel();
        updateContent2.Children.Add(new TextBlock { Text = "Flowseal zapret (конфиги)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        _updateStatus = new TextBlock { Text = "Проверка...", Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"), TextWrapping = TextWrapping.Wrap };
        updateContent2.Children.Add(_updateStatus);
        var updateBtn = new Button { Content = "Проверить обновления zapret", Style = (Style)Application.Current.FindResource("SecondaryButton"), Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        updateBtn.Click += async (_, _) => await CheckZapretUpdatesAsync();
        updateContent2.Children.Add(updateBtn);
        zapretUpdateCard.Child = updateContent2;
        root.Children.Add(zapretUpdateCard);

        var infoCard = CreateCard();
        var info = new StackPanel();
        info.Children.Add(new TextBlock { Text = "Быстрые подсказки", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        info.Children.Add(Muted("• Подробный порядок настройки — раздел «Гайд»"));
        info.Children.Add(Muted("• Тест стратегий: Сервис → Тестирование, номера 1,5,8,11,19 (Ростелеком/МГТС)"));
        info.Children.Add(Muted("• Настройте Secure DNS; не проверяйте обход в Яндекс.Браузере"));
        info.Children.Add(Muted("• Путь без кириллицы, например C:\\zapret"));
        infoCard.Child = info;
        root.Children.Add(infoCard);

        scroll.Content = root;
        Content = scroll;

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            var running = _strategy.IsRunning();
            _statusIndicator.Fill = running
                ? (Brush)Application.Current.FindResource("SuccessBrush")
                : (Brush)Application.Current.FindResource("ErrorBrush");
            _statusLabel.Text = running ? " winws.exe запущен" : " winws.exe не запущен";
        };
        timer.Start();
    }

    private static Border CreateCard() => new()
    {
        Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
        BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(20),
        Margin = new Thickness(0, 0, 0, 16)
    };

    private async Task LoadSecurityStatusAsync(StackPanel panel)
    {
        var status = await new SecuritySetupService(_paths).CheckStatusAsync();
        panel.Children.Clear();

        if (status.IsFullyConfigured)
        {
            panel.Children.Add(CreateSecurityBanner(
                "Безопасность Windows",
                "Исключения антивируса и правила брандмауэра настроены.",
                "SuccessBrush"));
            return;
        }

        if (!status.CheckSucceeded)
        {
            panel.Children.Add(CreateSecurityBanner(
                "Проверка безопасности",
                status.Summary,
                "WarningBrush"));
            return;
        }

        var text = status.Summary;
        if (status.MissingExclusions.Count > 0)
            text += "\n• " + string.Join("\n• ", status.MissingExclusions);
        if (status.MissingFirewallPrograms.Count > 0)
            text += "\n• " + string.Join("\n• ", status.MissingFirewallPrograms);

        var banner = CreateSecurityBanner("Требуется настройка безопасности", text, "ErrorBrush");
        if (banner.Child is StackPanel sp)
        {
            var btn = new Button
            {
                Content = "Настроить антивирус и брандмауэр",
                Style = (Style)Application.Current.FindResource("PrimaryButton"),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            btn.Click += (_, _) =>
            {
                if (Application.Current.MainWindow is MainWindow mw)
                    mw.RunSecuritySetup();
            };
            sp.Children.Add(btn);
        }
        panel.Children.Add(banner);
    }

    private static Border CreateSecurityBanner(string title, string description, string brushKey)
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            BorderBrush = (Brush)Application.Current.FindResource(brushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource(brushKey)
        });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        border.Child = stack;
        return border;
    }

    private static Border CreateHintBanner(SystemHint hint)
    {
        var brushKey = hint.Level switch
        {
            HintLevel.Error => "ErrorBrush",
            HintLevel.Warning => "WarningBrush",
            _ => "AccentBrush"
        };
        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            BorderBrush = (Brush)Application.Current.FindResource(brushKey),
            BorderThickness = new Thickness(1, 1, 1, 1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = hint.Title,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource(brushKey)
        });
        stack.Children.Add(new TextBlock
        {
            Text = hint.Description,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        border.Child = stack;
        return border;
    }

    private static TextBlock Muted(string text) => new()
    {
        Text = text,
        Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 2, 0, 2)
    };

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
            ConsoleLog.Instance.Write($"Запуск стратегии: {strategy}");
            await _strategy.StartStrategyAsync(strategy);
            ConsoleLog.Instance.Write("Стратегия запущена");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private async Task StopAsync()
    {
        await _strategy.StopStrategyAsync();
        ConsoleLog.Instance.Write("winws.exe остановлен");
    }

    private async Task CheckZapretUpdatesAsync()
    {
        _updateStatus.Text = "Проверка обновлений zapret...";
        var result = await _updates.CheckForUpdatesAsync();
        if (result.Error is not null)
        {
            _updateStatus.Text = $"Не удалось проверить: {result.Error}";
            return;
        }
        if (result.IsUpToDate)
            _updateStatus.Text = $"Zapret актуален: {result.LocalVersion}";
        else
            _updateStatus.Text = $"Доступна версия zapret {result.RemoteVersion} (у вас {result.LocalVersion}). Обновите в разделе «Сервис».";
    }

    private async Task CheckAppUpdatesAsync(bool silent)
    {
        if (!silent)
            _appUpdateStatus.Text = "Проверка обновлений Zapret UI...";

        var check = await _appUpdater.CheckForUpdateAsync();
        if (check.Error is not null)
        {
            _appUpdateStatus.Text = silent
                ? $"Zapret UI v{check.LocalVersion}"
                : $"Ошибка: {check.Error}";
            return;
        }

        if (!check.HasUpdate || check.Manifest is null)
        {
            if (!silent)
                _appUpdateStatus.Text = $"Zapret UI v{check.LocalVersion} — актуальная версия";
            return;
        }

        if (silent)
        {
            _appUpdateStatus.Text = $"Доступна v{check.RemoteVersion}";
            return;
        }

        _appUpdateStatus.Text = $"Доступна v{check.RemoteVersion} (у вас v{check.LocalVersion})";

        if (!UiHelpers.Confirm($"Установить Zapret UI {check.RemoteVersion}? Программа перезапустится."))
            return;

        var install = await _appUpdater.InstallUpdateAsync(check.Manifest);
        if (install.Success && install.RequiresRestart)
            Application.Current.Shutdown();
        else if (!install.Success)
            UiHelpers.ShowError(install.Message);
        else
            _appUpdateStatus.Text = install.Message;
    }
}
