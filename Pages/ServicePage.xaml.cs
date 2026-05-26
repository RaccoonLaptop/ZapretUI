using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class ServicePage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly ServiceSettingsService _settings;
    private readonly UpdateService _updates;
    private readonly ProcessRunner _runner;
    private TextBlock _gameFilterStatus = null!;
    private TextBlock _ipsetStatus = null!;
    private TextBlock _autoUpdateStatus = null!;
    private ComboBox _strategyCombo = null!;

    public ServicePage(ZapretPaths paths, StrategyService strategy)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = new ServiceSettingsService(paths);
        _updates = new UpdateService(paths);
        _runner = new ProcessRunner();
        _runner.SetZapretRoot(paths.Root);
        _runner.OutputReceived += line => ConsoleLog.Instance.Write(line);
        BuildUi();
        RefreshStatuses();
    }

    private void BuildUi()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel();

        root.Children.Add(new TextBlock { Text = "Сервис", FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        root.Children.Add(new TextBlock
        {
            Text = "Функции service.bat — установка службы, настройки, обновления, диагностика",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Service management
        root.Children.Add(Section("Управление службой"));
        var svcCard = Card();
        var svcStack = new StackPanel();

        svcStack.Children.Add(Label("Стратегия для автозапуска:"));
        _strategyCombo = new ComboBox { MinWidth = 350, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var s in _paths.GetStrategyFiles()) _strategyCombo.Items.Add(s);
        if (_strategyCombo.Items.Count > 0) _strategyCombo.SelectedIndex = 0;
        svcStack.Children.Add(_strategyCombo);

        var svcBtns = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        svcBtns.Children.Add(ActionBtn("Установить службу", async () => await RunBridge("InstallService", GetSelectedStrategy())));
        svcBtns.Children.Add(ActionBtn("Удалить службы", async () => await RunBridge("RemoveServices")));
        svcBtns.Children.Add(ActionBtn("Проверить статус", async () => await RunBridge("CheckStatus"), true));
        svcStack.Children.Add(svcBtns);
        svcCard.Child = svcStack;
        root.Children.Add(svcCard);

        // Settings toggles
        root.Children.Add(Section("Настройки"));
        var setCard = Card();
        var setStack = new StackPanel();

        _gameFilterStatus = StatusLine("Game Filter");
        setStack.Children.Add(_gameFilterStatus);
        var gameBtns = new WrapPanel { Margin = new Thickness(0, 4, 0, 12) };
        gameBtns.Children.Add(ActionBtn("Выключить", () => { _settings.SetGameFilter("disabled"); RefreshStatuses(); }));
        gameBtns.Children.Add(ActionBtn("TCP+UDP", () => { _settings.SetGameFilter("all"); RefreshStatuses(); }));
        gameBtns.Children.Add(ActionBtn("Только TCP", () => { _settings.SetGameFilter("tcp"); RefreshStatuses(); }));
        gameBtns.Children.Add(ActionBtn("Только UDP", () => { _settings.SetGameFilter("udp"); RefreshStatuses(); }));
        setStack.Children.Add(gameBtns);

        _ipsetStatus = StatusLine("IPSet Filter");
        setStack.Children.Add(_ipsetStatus);
        setStack.Children.Add(ActionBtn("Переключить режим IPSet", () =>
        {
            try { _settings.CycleIpsetFilter(); RefreshStatuses(); ConsoleLog.Instance.Write("IPSet режим переключён"); }
            catch (Exception ex) { UiHelpers.ShowError(ex.Message); }
        }));

        _autoUpdateStatus = StatusLine("Auto-Update");
        setStack.Children.Add(_autoUpdateStatus);
        setStack.Children.Add(ActionBtn("Переключить авто-обновление", () => { _settings.ToggleAutoUpdate(); RefreshStatuses(); }));

        setCard.Child = setStack;
        root.Children.Add(setCard);

        // Updates
        root.Children.Add(Section("Обновления"));
        var updCard = Card();
        var updBtns = new WrapPanel();
        updBtns.Children.Add(ActionBtn("Обновить IPSet List", async () =>
        {
            ConsoleLog.Instance.Write("Обновление ipset-all.txt...");
            try { await _updates.UpdateIpsetListAsync(); ConsoleLog.Instance.Write("IPSet обновлён"); RefreshStatuses(); }
            catch (Exception ex) { ConsoleLog.Instance.Write($"Ошибка: {ex.Message}"); }
        }));
        updBtns.Children.Add(ActionBtn("Обновить Hosts File", async () => await RunBridge("UpdateHosts"), true));
        updBtns.Children.Add(ActionBtn("Проверить обновления", async () =>
        {
            var r = await _updates.CheckForUpdatesAsync();
            if (r.Error is not null) ConsoleLog.Instance.Write($"Ошибка: {r.Error}");
            else if (r.IsUpToDate) ConsoleLog.Instance.Write($"Версия актуальна: {r.LocalVersion}");
            else ConsoleLog.Instance.Write($"Доступна версия {r.RemoteVersion} (у вас {r.LocalVersion})");
        }));
        updBtns.Children.Add(ActionBtn("Открыть страницу релиза", () => _updates.OpenReleasePage()));
        updCard.Child = updBtns;
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
        secStack.Children.Add(ActionBtn("🛡 Настроить антивирус и брандмауэр", () =>
        {
            if (Application.Current.MainWindow is MainWindow mw)
                mw.RunSecuritySetup();
        }));
        secCard.Child = secStack;
        root.Children.Add(secCard);

        root.Children.Add(new TextBlock
        {
            Text = "Диагностика и тесты — в отдельном окне «Консоль» (меню слева).",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        });

        scroll.Content = root;
        Content = scroll;
    }

    private void RefreshStatuses()
    {
        _gameFilterStatus.Text = $"Game Filter: {_settings.GetGameFilterStatus()}";
        _ipsetStatus.Text = $"IPSet Filter: {_settings.GetIpsetStatus()}";
        _autoUpdateStatus.Text = $"Auto-Update Check: {(_settings.IsAutoUpdateEnabled() ? "enabled" : "disabled")}";
    }

    private string GetSelectedStrategy() =>
        _strategyCombo.SelectedItem as string ?? "general.bat";

    private async Task RunBridge(string action, string? extra = null)
    {
        ConsoleLog.Instance.Write($"--- {action} ---");
        try
        {
            await _runner.RunBridgeAsync(action, extra);
            ConsoleLog.Instance.Write($"--- {action} завершено ---");
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка: {ex.Message}");
        }
    }

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

    private Button ActionBtn(string text, Action action) => ActionBtn(text, () => { action(); return Task.CompletedTask; }, false);

    private Button ActionBtn(string text, Func<Task> action, bool toConsole = false)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 8)
        };
        btn.Click += async (_, _) =>
        {
            if (toConsole) ConsoleLog.Instance.Write($"Запуск: {text}");
            await action();
        };
        return btn;
    }
}
