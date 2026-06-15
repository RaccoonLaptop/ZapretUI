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
    private Button _gameDisabledBtn = null!;
    private Button _gameTcpUdpBtn = null!;
    private Button _gameTcpBtn = null!;
    private Button _gameUdpBtn = null!;
    private Button _ipsetLoadedBtn = null!;
    private Button _ipsetNoneBtn = null!;
    private Button _ipsetAnyBtn = null!;
    private ComboBox _strategyCombo = null!;
    private CheckBox _uiStartupCheck = null!;

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

        root.Children.Add(new TextBlock { Text = Loc.T("service.title"), FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("service.subtitle"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Service
        root.Children.Add(Section(Loc.T("service.section_service")));
        var svcCard = Card();
        var svcStack = new StackPanel();
        svcStack.Children.Add(Label(Loc.T("service.strategy_autostart")));
        _strategyCombo = new ComboBox { MinWidth = 350, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var s in _paths.GetStrategyFiles()) _strategyCombo.Items.Add(s);
        if (_strategyCombo.Items.Count > 0) _strategyCombo.SelectedIndex = 0;
        svcStack.Children.Add(_strategyCombo);

        _uiStartupCheck = new CheckBox
        {
            Content = Loc.T("service.start_ui_tray"),
            IsChecked = AppStartupService.IsEnabled(),
            Margin = new Thickness(0, 0, 0, 12)
        };
        _uiStartupCheck.Checked += (_, _) =>
        {
            if (_uiStartupCheck.IsChecked == true)
            {
                AppStartupService.Enable();
                _settings.StartUiOnLogin = true;
            }
            else
            {
                AppStartupService.Disable();
                _settings.StartUiOnLogin = false;
            }
            _settings.Save();
        };
        _uiStartupCheck.Unchecked += (_, _) =>
        {
            if (_uiStartupCheck.IsChecked != true)
            {
                AppStartupService.Disable();
                _settings.StartUiOnLogin = false;
                _settings.Save();
            }
        };
        svcStack.Children.Add(_uiStartupCheck);

        var svcBtns = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        svcBtns.Children.Add(ActionBtn(Loc.T("service.install_service"), async () => await InstallServiceAsync()));
        svcBtns.Children.Add(ActionBtn(Loc.T("service.remove_services"), async () => await RemoveServicesAsync()));
        svcBtns.Children.Add(ActionBtn(Loc.T("service.check_status"), async () => await RunBridgeWithDialog(Loc.T("dialog.service_status"), "CheckStatus")));
        svcStack.Children.Add(svcBtns);
        svcCard.Child = svcStack;
        root.Children.Add(svcCard);

        // Settings
        root.Children.Add(Section(Loc.T("service.section_settings")));
        var setCard = Card();
        var setStack = new StackPanel();

        _gameFilterStatus = StatusLine(Loc.T("service.game_filter"));
        setStack.Children.Add(_gameFilterStatus);
        var gameBtns = new WrapPanel { Margin = new Thickness(0, 4, 0, 12) };
        _gameDisabledBtn = SettingsBtn(Loc.T("service.disable"), () => _settingsSvc.SetGameFilter("disabled"));
        _gameTcpUdpBtn = SettingsBtn(Loc.T("service.tcp_udp"), () => _settingsSvc.SetGameFilter("all"));
        _gameTcpBtn = SettingsBtn(Loc.T("service.tcp_only"), () => _settingsSvc.SetGameFilter("tcp"));
        _gameUdpBtn = SettingsBtn(Loc.T("service.udp_only"), () => _settingsSvc.SetGameFilter("udp"));
        gameBtns.Children.Add(_gameDisabledBtn);
        gameBtns.Children.Add(_gameTcpUdpBtn);
        gameBtns.Children.Add(_gameTcpBtn);
        gameBtns.Children.Add(_gameUdpBtn);
        setStack.Children.Add(gameBtns);

        _ipsetStatus = StatusLine(Loc.T("service.ipset_filter"));
        setStack.Children.Add(_ipsetStatus);
        var ipsetBtns = new WrapPanel { Margin = new Thickness(0, 4, 0, 12) };
        _ipsetLoadedBtn = SettingsBtn(Loc.T("service.ipset_loaded"), () => SetIpsetMode("loaded"));
        _ipsetNoneBtn = SettingsBtn(Loc.T("service.ipset_none"), () => SetIpsetMode("none"));
        _ipsetAnyBtn = SettingsBtn(Loc.T("service.ipset_any"), () => SetIpsetMode("any"));
        ipsetBtns.Children.Add(_ipsetLoadedBtn);
        ipsetBtns.Children.Add(_ipsetNoneBtn);
        ipsetBtns.Children.Add(_ipsetAnyBtn);
        setStack.Children.Add(ipsetBtns);

        var dataBtns = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
        dataBtns.Children.Add(ActionBtn(Loc.T("service.update_ipset"), async () => await UpdateIpsetWithDialog()));
        dataBtns.Children.Add(ActionBtn(Loc.T("service.update_hosts"), async () => await RunBridgeWithDialog(Loc.T("dialog.update_hosts"), "UpdateHosts")));
        setStack.Children.Add(dataBtns);

        setCard.Child = setStack;
        root.Children.Add(setCard);

        // Updates
        root.Children.Add(Section(Loc.T("service.section_updates")));
        var updCard = Card();
        var updStack = new StackPanel();
        updStack.Children.Add(new TextBlock
        {
            Text = Loc.T("service.updates_hint"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        var startupCheck = new CheckBox
        {
            Content = Loc.T("service.check_on_startup"),
            IsChecked = _settings.CheckUpdatesOnStartup,
            Margin = new Thickness(0, 0, 0, 12)
        };
        startupCheck.Checked += (_, _) => { _settings.CheckUpdatesOnStartup = true; _settings.Save(); };
        startupCheck.Unchecked += (_, _) => { _settings.CheckUpdatesOnStartup = false; _settings.Save(); };
        updStack.Children.Add(startupCheck);
        var updBtns = new WrapPanel();
        updBtns.Children.Add(ActionBtn(Loc.T("service.check_app_update"), async () => await CheckAppUpdateAsync()));
        updBtns.Children.Add(ActionBtn(Loc.T("service.check_flowseal_update"), async () => await CheckFlowsealUpdateAsync()));
        updBtns.Children.Add(ActionBtn(Loc.T("service.reinstall_flowseal"), async () => await ReinstallFlowsealAsync()));
        updBtns.Children.Add(ActionBtn(Loc.T("service.open_flowseal_release"), () => OpenUrl(_updates.GetReleaseUrl())));
        updStack.Children.Add(updBtns);
        updCard.Child = updStack;
        root.Children.Add(updCard);

        // Network reset
        root.Children.Add(Section(Loc.T("service.section_network")));
        var netCard = Card();
        var netStack = new StackPanel();
        netStack.Children.Add(new TextBlock
        {
            Text = Loc.T("service.network_desc"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        netStack.Children.Add(new TextBlock
        {
            Text = Loc.T("service.network_cmds"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 12)
        });
        netStack.Children.Add(ActionBtn(Loc.T("service.reset_network"), async () => await ResetNetworkAsync()));
        netCard.Child = netStack;
        root.Children.Add(netCard);

        // Security
        root.Children.Add(Section(Loc.T("service.section_security")));
        var secCard = Card();
        var secStack = new StackPanel();
        secStack.Children.Add(new TextBlock
        {
            Text = Loc.T("service.security_desc"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        secStack.Children.Add(ActionBtn(Loc.T("service.setup_security"), async () => await HandleSecurityAsync()));
        secCard.Child = secStack;
        root.Children.Add(secCard);

        // Language
        root.Children.Add(Section(Loc.T("service.section_language")));
        var langCard = Card();
        var langStack = new StackPanel();
        langStack.Children.Add(new TextBlock
        {
            Text = Loc.T("service.language_desc"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        var langCombo = new ComboBox
        {
            MinWidth = 240,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        langCombo.Items.Add(Loc.T("service.lang_ru"));
        langCombo.Items.Add(Loc.T("service.lang_en"));
        langCombo.SelectedIndex = string.Equals(_settings.Language, "en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        langCombo.SelectionChanged += (_, _) =>
        {
            var newLang = langCombo.SelectedIndex == 1 ? "en" : "ru";
            if (string.Equals(_settings.Language, newLang, StringComparison.OrdinalIgnoreCase)) return;
            _settings.Language = newLang;
            _settings.Save();
            LocalizationService.Initialize(newLang);
            if (UiHelpers.Confirm(Loc.T("lang.restart_confirm"), OwnerWindow))
                LocalizationService.RestartApplication();
        };
        langStack.Children.Add(langCombo);
        langCard.Child = langStack;
        root.Children.Add(langCard);

        // Links
        root.Children.Add(Section(Loc.T("service.section_links")));
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
            UiHelpers.ShowInfo(Loc.F("service.security_all_ok", status.Summary));
            return;
        }

        if (!status.CheckSucceeded)
        {
            if (UiHelpers.Confirm(Loc.F("service.security_check_failed", status.Summary)))
            {
                if (Application.Current.MainWindow is MainWindow mw)
                    mw.RunSecuritySetup();
            }
            return;
        }

        var details = status.Summary;
        if (status.MissingExclusions.Count > 0)
            details += "\n\n" + Loc.F("service.security_defender", string.Join(", ", status.MissingExclusions));
        if (status.MissingFirewallPrograms.Count > 0)
            details += "\n\n" + Loc.F("service.security_firewall", string.Join(", ", status.MissingFirewallPrograms));

        if (UiHelpers.Confirm(Loc.F("service.security_issues", details)))
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
                UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_app"), $"{Loc.T("common.error_prefix")} {result.Error}");
                return;
            }

            if (!result.HasUpdate)
            {
                UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_app"), Loc.F("update.app_up_to_date", result.LocalVersion));
                return;
            }

            if (UiHelpers.Confirm(Loc.F("update.app_available", result.RemoteVersion, result.LocalVersion), OwnerWindow))
            {
                if (result.Manifest is not null)
                {
                    PreparedAppUpdate? prepared = null;
                    var keepPrepared = false;
                    UpdateDownloadWindow? progressWin = new UpdateDownloadWindow(Loc.T("update.download_title"), Loc.T("update.download_status"));
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
                        progressWin.SetStatus(Loc.T("update.download_complete"));

                        if (UiHelpers.Confirm(Loc.F("update.app_ready", result.RemoteVersion), OwnerWindow))
                        {
                            progressWin.SetStatus(Loc.T("update.starting_install"));
                            progressWin.ReportProgress(new DownloadProgress
                            {
                                Phase = Loc.T("update.install_phase")
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
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_app"), $"{Loc.T("common.error_prefix")} {ex.Message}");
        }
    }

    private async Task ReinstallFlowsealAsync()
    {
        if (!UiHelpers.Confirm(Loc.T("update.flowseal_reinstall"), OwnerWindow))
            return;

        await FlowsealReinstallService.ReinstallAsync(OwnerWindow, _paths);
    }

    private async Task CheckFlowsealUpdateAsync()
    {
        var r = await _updates.CheckForUpdatesAsync();
        if (r.Error is not null)
        {
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_flowseal"), $"{Loc.T("common.error_prefix")} {r.Error}");
            return;
        }

        if (r.IsUpToDate)
        {
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_flowseal"), Loc.F("update.flowseal_up_to_date", r.LocalVersion));
            return;
        }

        if (UiHelpers.Confirm(Loc.F("update.flowseal_available", r.RemoteVersion, r.LocalVersion), OwnerWindow))
            await FlowsealReinstallService.ReinstallAsync(OwnerWindow, _paths);
    }

    private async Task ResetNetworkAsync()
    {
        if (!UiHelpers.Confirm(Loc.T("network.reset_confirm"), OwnerWindow))
            return;

        try
        {
            var (ok, output) = await NetworkResetService.RunAllAsync();
            UiHelpers.ShowResult(OwnerWindow, Loc.T("network.reset_title"), output + (ok ? Loc.T("network.reset_reboot") : ""));
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, Loc.T("network.reset_title"), $"{Loc.T("common.error_prefix")} {ex.Message}");
        }
    }

    private async Task UpdateIpsetWithDialog()
    {
        try
        {
            await _updates.UpdateIpsetListAsync();
            RefreshStatuses();
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.ipset_list"), Loc.T("dialog.ipset_ok"));
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.ipset_list"), $"{Loc.T("common.error_prefix")} {ex.Message}");
        }
    }

    private async Task InstallServiceAsync()
    {
        try
        {
            var result = await _runner.RunBridgeAsync("InstallService", GetSelectedStrategy());
            if (_uiStartupCheck.IsChecked == true)
            {
                AppStartupService.Enable();
                _settings.StartUiOnLogin = true;
                _settings.Save();
                result += "\n\n" + Loc.T("service.ui_tray_enabled");
            }
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.install_service"), result);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.install_service"), $"{Loc.T("common.error_prefix")} {ex.Message}");
        }
    }

    private async Task RemoveServicesAsync()
    {
        try
        {
            var result = await _runner.RunBridgeAsync("RemoveServices");
            AppStartupService.Disable();
            _settings.StartUiOnLogin = false;
            _settings.Save();
            _uiStartupCheck.IsChecked = false;
            result += "\n\n" + Loc.T("service.ui_tray_disabled");
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.remove_services"), result);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.remove_services"), $"{Loc.T("common.error_prefix")} {ex.Message}");
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
            UiHelpers.ShowResult(OwnerWindow, title, $"{Loc.T("common.error_prefix")} {ex.Message}");
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

    private void SetIpsetMode(string mode)
    {
        try
        {
            _settingsSvc.SetIpsetFilter(mode);
            RefreshStatuses();
            ConsoleLog.Instance.Write(Loc.F("service.ipset_set", mode));
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void RefreshStatuses()
    {
        SetStatusText(_gameFilterStatus, "Game Filter", _settingsSvc.GetGameFilterStatus());
        SetStatusText(_ipsetStatus, "IPSet Filter", _settingsSvc.GetIpsetStatus());

        var gameMode = _settingsSvc.GetGameFilterMode();
        ApplyActiveStyle(_gameDisabledBtn, gameMode == "disabled");
        ApplyActiveStyle(_gameTcpUdpBtn, gameMode == "all");
        ApplyActiveStyle(_gameTcpBtn, gameMode == "tcp");
        ApplyActiveStyle(_gameUdpBtn, gameMode == "udp");

        var ipsetMode = _settingsSvc.GetIpsetStatus();
        ApplyActiveStyle(_ipsetLoadedBtn, ipsetMode == "loaded");
        ApplyActiveStyle(_ipsetNoneBtn, ipsetMode == "none");
        ApplyActiveStyle(_ipsetAnyBtn, ipsetMode == "any");
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
        if (normalized is "loaded" or "any")
            return (Brush)Application.Current.FindResource("SuccessBrush");
        if (normalized is "none")
            return (Brush)Application.Current.FindResource("WarningBrush");
        return (Brush)Application.Current.FindResource("AccentBrush");
    }

    private Button SettingsBtn(string text, Action action)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 8)
        };
        btn.Click += (_, _) =>
        {
            action();
            RefreshStatuses();
        };
        return btn;
    }

    private static void ApplyActiveStyle(Button btn, bool active) =>
        btn.Style = (Style)Application.Current.FindResource(active ? "SuccessButton" : "SecondaryButton");

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
