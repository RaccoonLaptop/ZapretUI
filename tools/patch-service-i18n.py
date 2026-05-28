from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Pages" / "ServicePage.xaml.cs"
text = p.read_text(encoding="utf-8")

replacements = [
    ('Text = "Сервис"', 'Text = Loc.T("service.title")'),
    ('Text = "Управление службой, настройки фильтров, обновления и безопасность"', 'Text = Loc.T("service.subtitle")'),
    ('Section("Управление службой")', 'Section(Loc.T("service.section_service"))'),
    ('Label("Стратегия для автозапуска:")', 'Label(Loc.T("service.strategy_autostart"))'),
    ('ActionBtn("Установить службу", async () => await RunBridgeWithDialog("Установка службы", "InstallService", GetSelectedStrategy()))',
     'ActionBtn(Loc.T("service.install_service"), async () => await RunBridgeWithDialog(Loc.T("dialog.install_service"), "InstallService", GetSelectedStrategy()))'),
    ('ActionBtn("Удалить службы", async () => await RunBridgeWithDialog("Удаление служб", "RemoveServices"))',
     'ActionBtn(Loc.T("service.remove_services"), async () => await RunBridgeWithDialog(Loc.T("dialog.remove_services"), "RemoveServices"))'),
    ('ActionBtn("Проверить статус", async () => await RunBridgeWithDialog("Статус службы", "CheckStatus"))',
     'ActionBtn(Loc.T("service.check_status"), async () => await RunBridgeWithDialog(Loc.T("dialog.service_status"), "CheckStatus"))'),
    ('Section("Настройки")', 'Section(Loc.T("service.section_settings"))'),
    ('StatusLine("Game Filter")', 'StatusLine(Loc.T("service.game_filter"))'),
    ('ActionBtn("Выключить",', 'ActionBtn(Loc.T("service.disable"),'),
    ('ActionBtn("Только TCP",', 'ActionBtn(Loc.T("service.tcp_only"),'),
    ('ActionBtn("Только UDP",', 'ActionBtn(Loc.T("service.udp_only"),'),
    ('StatusLine("IPSet Filter")', 'StatusLine(Loc.T("service.ipset_filter"))'),
    ('ActionBtn("Переключить режим IPSet",', 'ActionBtn(Loc.T("service.toggle_ipset"),'),
    ('ConsoleLog.Instance.Write("IPSet режим переключён")', 'ConsoleLog.Instance.Write(Loc.T("service.ipset_toggled"))'),
    ('StatusLine("Auto-Update (Flowseal)")', 'StatusLine(Loc.T("service.auto_update"))'),
    ('ActionBtn("Переключить авто-обновление",', 'ActionBtn(Loc.T("service.toggle_autoupdate"),'),
    ('ActionBtn("Обновить IPSet List",', 'ActionBtn(Loc.T("service.update_ipset"),'),
    ('ActionBtn("Обновить Hosts File", async () => await RunBridgeWithDialog("Обновление Hosts", "UpdateHosts"))',
     'ActionBtn(Loc.T("service.update_hosts"), async () => await RunBridgeWithDialog(Loc.T("dialog.update_hosts"), "UpdateHosts"))'),
    ('Section("Обновления")', 'Section(Loc.T("service.section_updates"))'),
    ('Text = "При запуске проверяются версии Zapret UI и Flowseal. Установка — только после вашего подтверждения."',
     'Text = Loc.T("service.updates_hint")'),
    ('Content = "Проверять обновления при запуске программы"', 'Content = Loc.T("service.check_on_startup")'),
    ('ActionBtn("Проверить обновление Zapret UI",', 'ActionBtn(Loc.T("service.check_app_update"),'),
    ('ActionBtn("Проверить обновление Flowseal",', 'ActionBtn(Loc.T("service.check_flowseal_update"),'),
    ('ActionBtn("Переустановить Flowseal",', 'ActionBtn(Loc.T("service.reinstall_flowseal"),'),
    ('ActionBtn("Открыть релиз Flowseal",', 'ActionBtn(Loc.T("service.open_flowseal_release"),'),
    ('Section("Сброс сетевых настроек")', 'Section(Loc.T("service.section_network"))'),
    ('ActionBtn("Сбросить сетевые настройки",', 'ActionBtn(Loc.T("service.reset_network"),'),
    ('Section("Безопасность Windows")', 'Section(Loc.T("service.section_security"))'),
    ('Text = "Исключения антивируса (Defender) и правила брандмауэра для Zapret UI и winws.exe."',
     'Text = Loc.T("service.security_desc")'),
    ('ActionBtn("Настроить антивирус и брандмауэр",', 'ActionBtn(Loc.T("service.setup_security"),'),
    ('Section("Ссылки")', 'Section(Loc.T("service.section_links"))'),
]

for old, new in replacements:
    text = text.replace(old, new)

text = text.replace(
    'Text = "Обязательный шаг, если раньше использовались VPN или прописывался прокси. " +\n'
    '                   "Они оставляют следы в системе — из‑за этого, например, Epic Games может писать " +\n'
    '                   "«находится в автономном режиме», хотя интернет есть."',
    'Text = Loc.T("service.network_desc")')
text = text.replace(
    'Text = "Выполняются: netsh int ip reset, winhttp reset proxy, winsock reset, " +\n'
    '                   "сброс IPv4/IPv6, диапазон TCP 10000–30000, ipconfig /flushdns."',
    'Text = Loc.T("service.network_cmds")')

text = text.replace(
    'if (UiHelpers.Confirm(\n'
    '                    $"Доступна новая версия Zapret UI: {result.RemoteVersion}\\nУ вас: {result.LocalVersion}\\n\\nСкачать обновление?",\n'
    '                    OwnerWindow))',
    'if (UiHelpers.Confirm(Loc.F("update.app_available", result.RemoteVersion, result.LocalVersion), OwnerWindow))')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI", $"Ошибка: {result.Error}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_app"), $"{Loc.T("common.error_prefix")} {result.Error}")')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI",\n'
    '                    $"Zapret UI актуален.\\nВерсия: {result.LocalVersion}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_app"), Loc.F("update.app_up_to_date", result.LocalVersion))')

text = text.replace(
    'UpdateDownloadWindow? progressWin = new UpdateDownloadWindow("Обновление Zapret UI", "Загрузка пакета обновления...");',
    'UpdateDownloadWindow? progressWin = new UpdateDownloadWindow(Loc.T("update.download_title"), Loc.T("update.download_status"));')

text = text.replace(
    'if (UiHelpers.Confirm(\n'
    '                                $"Пакет обновления Zapret UI {result.RemoteVersion} загружен.\\n\\nУстановить сейчас?"))',
    'if (UiHelpers.Confirm(Loc.F("update.app_ready", result.RemoteVersion), OwnerWindow))')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Обновление Zapret UI", $"Ошибка: {ex.Message}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_app"), $"{Loc.T("common.error_prefix")} {ex.Message}")')

text = text.replace(
    'if (!UiHelpers.Confirm(\n'
    '                "Переустановить компоненты Flowseal из GitHub?\\n\\n" +\n'
    '                "Папка zapret будет загружена заново (включая utils\\\\test zapret.ps1).\\n" +\n'
    '                "Ваши правки в .bat и lists могут быть перезаписаны."))',
    'if (!UiHelpers.Confirm(Loc.T("update.flowseal_reinstall"), OwnerWindow))')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Обновление Flowseal", $"Ошибка: {r.Error}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_flowseal"), $"{Loc.T("common.error_prefix")} {r.Error}")')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Обновление Flowseal", $"Flowseal актуален.\\nВерсия: {r.LocalVersion}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.update_flowseal"), Loc.F("update.flowseal_up_to_date", r.LocalVersion))')

text = text.replace(
    'if (UiHelpers.Confirm(\n'
    '                $"Доступна новая версия Flowseal: {r.RemoteVersion}\\nУ вас: {r.LocalVersion}\\n\\nПереустановить компоненты zapret?"))',
    'if (UiHelpers.Confirm(Loc.F("update.flowseal_available", r.RemoteVersion, r.LocalVersion), OwnerWindow))')

text = text.replace(
    'if (!UiHelpers.Confirm(\n'
    '                "Сбросить сетевые настройки Windows?\\n\\n" +\n'
    '                "Будут выполнены команды netsh и очистка DNS. " +\n'
    '                "Это помогает убрать следы VPN и прокси.\\n\\n" +\n'
    '                "Нужны права администратора. После сброса рекомендуется перезагрузка ПК.\\n\\n" +\n'
    '                "Продолжить?"))',
    'if (!UiHelpers.Confirm(Loc.T("network.reset_confirm"), OwnerWindow))')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Сброс сетевых настроек",\n'
    '                output + (ok ? "\\n\\nРекомендуется перезагрузить компьютер." : ""))',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("network.reset_title"), output + (ok ? Loc.T("network.reset_reboot") : ""))')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "Сброс сетевых настроек", $"Ошибка: {ex.Message}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("network.reset_title"), $"{Loc.T("common.error_prefix")} {ex.Message}")')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "IPSet List", "Список ipset-all.txt успешно обновлён из репозитория Flowseal.")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.ipset_list"), Loc.T("dialog.ipset_ok"))')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, "IPSet List", $"Ошибка: {ex.Message}")',
    'UiHelpers.ShowResult(OwnerWindow, Loc.T("dialog.ipset_list"), $"{Loc.T("common.error_prefix")} {ex.Message}")')

text = text.replace(
    'UiHelpers.ShowResult(OwnerWindow, title, $"Ошибка: {ex.Message}")',
    'UiHelpers.ShowResult(OwnerWindow, title, $"{Loc.T("common.error_prefix")} {ex.Message}")')

text = text.replace(
    '_settingsSvc.IsAutoUpdateEnabled() ? "enabled" : "disabled")',
    '_settingsSvc.IsAutoUpdateEnabled() ? Loc.T("status.enabled") : Loc.T("status.disabled"))')

lang_block = '''
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

'''
marker = '        // Links\n        root.Children.Add(Section(Loc.T("service.section_links")));'
if marker in text and 'service.section_language' not in text:
    text = text.replace(marker, lang_block + marker)

p.write_text(text, encoding="utf-8")
print("done")
