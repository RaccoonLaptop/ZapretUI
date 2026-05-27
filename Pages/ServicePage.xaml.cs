using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ZapretUI;
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
            Text = "При запуске проверяются версии Zapret UI и Flowseal. Установка — только после вашего подтверждения.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        var startupCheck = new CheckBox
        {
            Content = "Проверять обновления при запуске программы",
            IsChecked = _settings.CheckUpdatesOnStartup,
            Margin = new Thickness(0, 0, 0, 12)
        };
        startupCheck.Checked += (_, _) => { _settings.CheckUpdatesOnStartup = true; _settings.Save(); };
        startupCheck.Unchecked += (_, _) => { _settings.CheckUpdatesOnStartup = false; _settings.Save(); };
        updStack.Children.Add(startupCheck);
        var updBtns = new WrapPanel();
        updBtns.Children.Add(ActionBtn("Проверить обновление Zapret UI", async () => await CheckAppUpdateAsync()));
        updBtns.Children.Add(ActionBtn("Проверить обновление Flowseal", async () => await CheckFlowsealUpdateAsync()));
        updBtns.Children.Add(ActionBtn("Переустановить Flowseal", async () => await ReinstallFlowsealAsync()));
        updBtns.Children.Add(ActionBtn("Открыть релиз Flowseal", () => OpenUrl(_updates.GetReleaseUrl())));
        updStack.Children.Add(updBtns);
        updCard.Child = updStack;
        root.Children.Add(updCard);

        // Network reset
        root.Children.Add(Section("Сброс сетевых настроек"));
        var netCard = Card();
        var netStack = new StackPanel();
        netStack.Children.Add(new TextBlock
        {
            Text = "Обязательный шаг, если раньше использовались VPN или прописывался прокси. " +
                   "Они оставляют следы в системе — из‑за этого, например, Epic Games может писать " +
                   "«находится в автономном режиме», хотя интернет есть.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        netStack.Children.Add(new TextBlock
        {
            Text = "Выполняются: netsh int ip reset, winhttp reset proxy, winsock reset, " +
                   "сброс IPv4/IPv6, диапазон TCP 10000–30000, ipconfig /flushdns.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 12)
        });
        netStack.Children.Add(ActionBtn("Сбросить сетевые настройки", async () => await ResetNetworkAsync()));
        netCard.Child = netStack;
        root.Children.Add(netCard);

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
        var linksRow = new WrapPanel();
        linksRow.Children.Add(LinkButton("Flowseal/zapret-discord-youtube", FlowsealUrl));
        linksRow.Children.Add(LinkButton("Zapret UI (Niko)", AppUrl));
        linksCard.Child = linksRow;
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
            if (result.Error is not null)
            {
                UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI", $"Ошибка: {result.Error}");
                return;
            }

            if (!result.HasUpdate)
            {
                UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI",
                    $"Zapret UI актуален.\nВерсия: {result.LocalVersion}");
                return;
            }

            if (UiHelpers.Confirm(
                    $"Доступна новая версия Zapret UI: {result.RemoteVersion}\nУ вас: {result.LocalVersion}\n\nСкачать обновление?",
                    OwnerWindow))
            {
                if (result.Manifest is not null)
                {
                    PreparedAppUpdate? prepared = null;
                    var keepPrepared = false;
                    UpdateDownloadWindow? progressWin = new UpdateDownloadWindow("Обновление Zapret UI", "Загрузка пакета обновления...");
                    if (OwnerWindow is not null) progressWin.Owner = OwnerWindow;
                    progressWin.Show();
                    try
                    {
                        var downloadProgress = new Progress<DownloadProgress>(p => progressWin.ReportProgress(p));
                        var preparedResult = await updater.PrepareUpdateAsync(
                            result.Manifest, downloadProgress);
                        if (!preparedResult.Success || preparedResult.Payload is null)
                        {
                            UiHelpers.ShowError(preparedResult.Message);
                            return;
                        }

                        prepared = preparedResult.Payload;
                        progressWin.SetStatus("Загрузка завершена. Пакет готов к установке.");

                        if (UiHelpers.Confirm(
                                $"Пакет обновления Zapret UI {result.RemoteVersion} загружен.\n\nУстановить сейчас?"))
                        {
                            progressWin.SetStatus("Запуск установки…");
                            progressWin.ReportProgress(new DownloadProgress
                            {
                                Phase = "Сейчас откроется окно обновления"
                            });
                            var install = await updater.InstallPreparedUpdateAsync(prepared);
                            progressWin.Close();
                            progressWin = null!;
                            keepPrepared = install.KeepPreparedFiles;
                            if (install.RequiresRestart && Application.Current.MainWindow is MainWindow mw)
                                mw.ShutdownApplication();
                            else if (!install.Success)
                                UiHelpers.ShowError(install.Message);
                            else
                                UiHelpers.ShowInfo(install.Message);
                        }
                    }
                    finally
                    {
                        progressWin?.Close();
                        AppSelfUpdateService.CleanupPreparedUpdate(prepared, keepPrepared);
                    }
                }
            }
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

        await FlowsealReinstallService.ReinstallAsync(OwnerWindow, _paths);
    }

    private async Task CheckFlowsealUpdateAsync()
    {
        var r = await _updates.CheckForUpdatesAsync();
        if (r.Error is not null)
        {
            UiHelpers.ShowResult(OwnerWindow, "Обновление Flowseal", $"Ошибка: {r.Error}");
            return;
        }

        if (r.IsUpToDate)
        {
            UiHelpers.ShowResult(OwnerWindow, "Обновление Flowseal", $"Flowseal актуален.\nВерсия: {r.LocalVersion}");
            return;
        }

        if (UiHelpers.Confirm(
                $"Доступна новая версия Flowseal: {r.RemoteVersion}\nУ вас: {r.LocalVersion}\n\nПереустановить компоненты zapret?"))
            await FlowsealReinstallService.ReinstallAsync(OwnerWindow, _paths);
    }

    private async Task ResetNetworkAsync()
    {
        if (!UiHelpers.Confirm(
                "Сбросить сетевые настройки Windows?\n\n" +
                "Будут выполнены команды netsh и очистка DNS. " +
                "Это помогает убрать следы VPN и прокси.\n\n" +
                "Нужны права администратора. После сброса рекомендуется перезагрузка ПК.\n\n" +
                "Продолжить?"))
            return;

        try
        {
            var (ok, output) = await NetworkResetService.RunAllAsync();
            UiHelpers.ShowResult(OwnerWindow, "Сброс сетевых настроек",
                output + (ok ? "\n\nРекомендуется перезагрузить компьютер." : ""));
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, "Сброс сетевых настроек", $"Ошибка: {ex.Message}");
        }
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
        SetStatusText(_gameFilterStatus, "Game Filter", _settingsSvc.GetGameFilterStatus());
        SetStatusText(_ipsetStatus, "IPSet Filter", _settingsSvc.GetIpsetStatus());
        SetStatusText(_autoUpdateStatus, "Auto-Update Check",
            _settingsSvc.IsAutoUpdateEnabled() ? "enabled" : "disabled");
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
        Background = (Brush)Application.Current.FindResource("PanelOverlayBrush"),
        BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(16),
        Margin = new Thickness(0, 0, 0, 12)
    };

    private static TextBlock Label(string text) => new() { Text = text, Margin = new Thickness(0, 0, 0, 4) };

    private static TextBlock StatusLine(string name) => new()
    {
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 4)
    };

    private static void SetStatusText(TextBlock block, string label, string value)
    {
        block.Inlines.Clear();
        block.Inlines.Add(new Run($"{label}: ")
        {
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
        });
        block.Inlines.Add(new Run(value)
        {
            Foreground = GetStatusValueBrush(value),
            FontWeight = FontWeights.SemiBold
        });
    }

    private static Brush GetStatusValueBrush(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is "enabled" or "on" or "yes" or "true")
            return (Brush)Application.Current.FindResource("SuccessBrush");
        if (normalized is "disabled" or "off" or "no" or "false")
            return (Brush)Application.Current.FindResource("WarningBrush");
        return (Brush)Application.Current.FindResource("AccentBrush");
    }

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
