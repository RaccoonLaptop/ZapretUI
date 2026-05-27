using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class ServicePage : UserControl
{
    private const string FlowsealUrl = "https://github.com/Flowseal/zapret-discord-youtube";
    private const string AppUrl = "https://github.com/RaccoonLaptop/ZapretUI";

    private readonly ZapretPaths _paths;
    private readonly AppSettings _settings;
    private readonly ServiceSettingsService _settingsSvc;
    private readonly UpdateService _updates;
    private readonly ProcessRunner _runner;
    private TextBlock _gameFilterStatus = null!;
    private TextBlock _ipsetStatus = null!;
    private TextBlock _autoUpdateStatus = null!;
    private ComboBox _strategyCombo = null!;

    public ServicePage(ZapretPaths paths, StrategyService strategy)
    {
        _paths = paths;
        _ = strategy;
        _settings = AppSettings.Load();
        _settingsSvc = new ServiceSettingsService(paths);
        _updates = new UpdateService(paths);
        _runner = new ProcessRunner();
        _runner.SetZapretRoot(paths.Root);
        _runner.OutputReceived += line => ConsoleLog.Instance.Write(line);
        BuildUi();
        RefreshStatuses();
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private void BuildUi()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel();

        root.Children.Add(new TextBlock { Text = "Сервис", FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        root.Children.Add(new TextBlock
        {
            Text = "Управление службой, настройки фильтров, обновления и безопасность",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Service
        root.Children.Add(Section("Управление службой"));
        var svcCard = Card();
        var svcStack = new StackPanel();
        svcStack.Children.Add(Label("Стратегия для автозапуска:"));
        _strategyCombo = new ComboBox { MinWidth = 350, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var s in _paths.GetStrategyFiles()) _strategyCombo.Items.Add(s);
        if (_strategyCombo.Items.Count > 0) _strategyCombo.SelectedIndex = 0;
        svcStack.Children.Add(_strategyCombo);

        var svcBtns = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        svcBtns.Children.Add(ActionBtn("Установить службу", async () => await RunBridgeWithDialog("Установка службы", "InstallService", GetSelectedStrategy())));
        svcBtns.Children.Add(ActionBtn("Удалить службы", async () => await RunBridgeWithDialog("Удаление служб", "RemoveServices")));
        svcBtns.Children.Add(ActionBtn("Проверить статус", async () => await RunBridgeWithDialog("Статус службы", "CheckStatus")));
        svcStack.Children.Add(svcBtns);
        svcCard.Child = svcStack;
        root.Children.Add(svcCard);

        // Settings
        root.Children.Add(Section("Настройки"));
        var setCard = Card();
        var setStack = new StackPanel();

        _gameFilterStatus = StatusLine("Game Filter");
        setStack.Children.Add(_gameFilterStatus);
        var gameBtns = new WrapPanel { Margin = new Thickness(0, 4, 0, 12) };
        gameBtns.Children.Add(ActionBtn("Выключить", () => { _settingsSvc.SetGameFilter("disabled"); RefreshStatuses(); }));
        gameBtns.Children.Add(ActionBtn("TCP+UDP", () => { _settingsSvc.SetGameFilter("all"); RefreshStatuses(); }));
        gameBtns.Children.Add(ActionBtn("Только TCP", () => { _settingsSvc.SetGameFilter("tcp"); RefreshStatuses(); }));
        gameBtns.Children.Add(ActionBtn("Только UDP", () => { _settingsSvc.SetGameFilter("udp"); RefreshStatuses(); }));
        setStack.Children.Add(gameBtns);

        _ipsetStatus = StatusLine("IPSet Filter");
        setStack.Children.Add(_ipsetStatus);
        setStack.Children.Add(ActionBtn("Переключить режим IPSet", () =>
        {
            try { _settingsSvc.CycleIpsetFilter(); RefreshStatuses(); ConsoleLog.Instance.Write("IPSet режим переключён"); }
            catch (Exception ex) { UiHelpers.ShowError(ex.Message); }
        }));

        _autoUpdateStatus = StatusLine("Auto-Update (Flowseal)");
        setStack.Children.Add(_autoUpdateStatus);
        setStack.Children.Add(ActionBtn("Переключить авто-обновление", () => { _settingsSvc.ToggleAutoUpdate(); RefreshStatuses(); }));

        var dataBtns = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        dataBtns.Children.Add(ActionBtn("Обновить IPSet List", async () => await UpdateIpsetWithDialog()));
        dataBtns.Children.Add(ActionBtn("Обновить Hosts File", async () => await RunBridgeWithDialog("Обновление Hosts", "UpdateHosts")));
        setStack.Children.Add(dataBtns);

        setCard.Child = setStack;
        root.Children.Add(setCard);

        // Updates
        root.Children.Add(Section("Обновления"));
        var updCard = Card();
        var updStack = new StackPanel();
        updStack.Children.Add(new TextBlock
        {
            Text = "Проверка обновлений Zapret UI и компонентов Flowseal/zapret-discord-youtube",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        var updBtns = new WrapPanel();
        updBtns.Children.Add(ActionBtn("Проверить обновление Zapret UI", async () => await CheckAppUpdateAsync()));
        updBtns.Children.Add(ActionBtn("Проверить обновление Flowseal", async () => await CheckFlowsealUpdateAsync()));
        updBtns.Children.Add(ActionBtn("Переустановить Flowseal", async () => await ReinstallFlowsealAsync()));
        updBtns.Children.Add(ActionBtn("Открыть релиз Flowseal", () => OpenUrl(_updates.GetReleaseUrl())));
        updStack.Children.Add(updBtns);
        updCard.Child = updStack;
        root.Children.Add(updCard);

        // Security
        root.Children.Add(Section("Безопасность Windows"));
        var secCard = Card();
        var secStack = new StackPanel();
        secStack.Children.Add(new TextBlock
        {
            Text = "Исключения антивируса (Defender) и правила брандмауэра для Zapret UI и winws.exe.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        secStack.Children.Add(ActionBtn("Настроить антивирус и брандмауэр", async () => await HandleSecurityAsync()));
        secCard.Child = secStack;
        root.Children.Add(secCard);

        // Links
        root.Children.Add(Section("Ссылки"));
        var linksCard = Card();
        var linksStack = new StackPanel();
        linksStack.Children.Add(LinkButton("Flowseal/zapret-discord-youtube", FlowsealUrl));
        linksStack.Children.Add(LinkButton("Zapret UI (Niko)", AppUrl));
        linksCard.Child = linksStack;
        root.Children.Add(linksCard);

        scroll.Content = root;
        Content = scroll;
    }

    private async Task HandleSecurityAsync()
    {
        var security = new SecuritySetupService(_paths);
        var status = await security.CheckStatusAsync();

        if (status.IsFullyConfigured)
        {
            UiHelpers.ShowInfo($"Всё уже настроено.\n\n{status.Summary}");
            return;
        }

        if (!status.CheckSucceeded)
        {
            if (UiHelpers.Confirm($"{status.Summary}\n\nОткрыть мастер настройки?"))
            {
                if (Application.Current.MainWindow is MainWindow mw)
                    mw.RunSecuritySetup();
            }
            return;
        }

        var details = status.Summary;
        if (status.MissingExclusions.Count > 0)
            details += "\n\nDefender: " + string.Join(", ", status.MissingExclusions);
        if (status.MissingFirewallPrograms.Count > 0)
            details += "\n\nБрандмауэр: " + string.Join(", ", status.MissingFirewallPrograms);

        if (UiHelpers.Confirm($"Обнаружены проблемы:\n\n{details}\n\nДобавить автоматически?"))
        {
            if (Application.Current.MainWindow is MainWindow mw)
                mw.RunSecuritySetup();
            return;
        }

        UiHelpers.ShowInfo(security.GetManualInstructions());
    }

    private async Task CheckAppUpdateAsync()
    {
        try
        {
            var updater = new AppSelfUpdateService(_settings, _paths.Root);
            var result = await updater.CheckForUpdateAsync();
            var text = result.Error is not null
                ? $"Ошибка: {result.Error}"
                : result.HasUpdate
                    ? $"Доступна новая версия Zapret UI: {result.RemoteVersion}\nУ вас: {result.LocalVersion}\n\nОбновление можно установить при следующем запуске (если включено авто-обновление)."
                    : $"Zapret UI актуален.\nВерсия: {result.LocalVersion}";
            UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI", text);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI", $"Ошибка: {ex.Message}");
        }
    }

    private async Task ReinstallFlowsealAsync()
    {
        if (!UiHelpers.Confirm(
                "Переустановить компоненты Flowseal из GitHub?\n\n" +
                "Папка zapret будет загружена заново (включая utils\\test zapret.ps1).\n" +
                "Ваши правки в .bat и lists могут быть перезаписаны."))
            return;

        var target = _paths.Root;
        try
        {
            if (Directory.Exists(target))
                Directory.Delete(target, true);

            var bootstrap = new BootstrapWindow(target) { Owner = OwnerWindow };
            if (bootstrap.ShowDialog() != true)
            {
                UiHelpers.ShowResult(OwnerWindow, "Flowseal", "Установка отменена или не удалась.");
                return;
            }

            UiHelpers.ShowResult(OwnerWindow, "Flowseal",
                "Компоненты Flowseal переустановлены.\nПерезапустите Zapret UI для применения изменений.");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, "Flowseal", $"Ошибка: {ex.Message}");
        }
    }

    private async Task CheckFlowsealUpdateAsync()
    {
        var r = await _updates.CheckForUpdatesAsync();
        string text;
        if (r.Error is not null)
            text = $"Ошибка: {r.Error}";
        else if (r.IsUpToDate)
            text = $"Flowseal актуален.\nВерсия: {r.LocalVersion}";
        else
            text = $"Доступна новая версия Flowseal: {r.RemoteVersion}\nУ вас: {r.LocalVersion}\n\nСкачайте с GitHub или обновите через установщик.";
        UiHelpers.ShowResult(OwnerWindow, "Обновление Flowseal", text);
    }

    private async Task UpdateIpsetWithDialog()
    {
        try
        {
            await _updates.UpdateIpsetListAsync();
            RefreshStatuses();
            UiHelpers.ShowResult(OwnerWindow, "IPSet List", "Список ipset-all.txt успешно обновлён из репозитория Flowseal.");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, "IPSet List", $"Ошибка: {ex.Message}");
        }
    }

    private async Task RunBridgeWithDialog(string title, string action, string? extra = null)
    {
        try
        {
            var result = await _runner.RunBridgeAsync(action, extra);
            UiHelpers.ShowResult(OwnerWindow, title, result);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, title, $"Ошибка: {ex.Message}");
        }
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static Button LinkButton(string text, string url)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += (_, _) => OpenUrl(url);
        return btn;
    }

    private void RefreshStatuses()
    {
        _gameFilterStatus.Text = $"Game Filter: {_settingsSvc.GetGameFilterStatus()}";
        _ipsetStatus.Text = $"IPSet Filter: {_settingsSvc.GetIpsetStatus()}";
        _autoUpdateStatus.Text = $"Auto-Update Check: {(_settingsSvc.IsAutoUpdateEnabled() ? "enabled" : "disabled")}";
    }

    private string GetSelectedStrategy() =>
        _strategyCombo.SelectedItem as string ?? "general.bat";

    private static TextBlock Section(string text) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 8)
    };

    private static Border Card() => new()
    {
        Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
        BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(16),
        Margin = new Thickness(0, 0, 0, 12)
    };

    private static TextBlock Label(string text) => new() { Text = text, Margin = new Thickness(0, 0, 0, 4) };

    private static TextBlock StatusLine(string name) => new()
    {
        Text = $"{name}: ...",
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 4)
    };

    private Button ActionBtn(string text, Action action) => ActionBtn(text, () => { action(); return Task.CompletedTask; });

    private Button ActionBtn(string text, Func<Task> action)
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
}
