# Контекст проекта Zapret UI (для AI-агента)

## Кратко

**Zapret UI** — десктопное приложение Windows (WPF), обёртка над конфигами [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube). Пользователь выбирает `.bat`-стратегию и нажимает «Запустить» — в фоне стартует `winws.exe` (ядро zapret). Автор UI: **Niko**, репозиторий: **RaccoonLaptop/ZapretUI**.

## Что НЕ является этим проектом

- Это **не** форк zapret и **не** замена Flowseal — UI только управляет уже скачанными файлами Flowseal
- Старая копия в `zapret-discord-youtube-1.9.8c\ZapretUI` — **устарела**, работать нужно в `Desktop\ZapretUI`

## Структура на диске пользователя

```
%LocalAppData%\ZapretUI\
├── settings.json          # настройки UI (язык, фон, последняя стратегия, пути)
├── ZapretUI.exe           # программа (после установки)
├── Resources\             # Strings.ru.json, Strings.en.json
└── zapret\                # Flowseal-пакет (скачивается при первом запуске)
    ├── *.bat              # стратегии
    ├── bin\winws.exe
    ├── lists\
    └── utils\
        ├── game_filter.enabled
        └── .zapretui_defaults_v1   # маркер «дефолты применены»
```

## Главные сценарии

| Действие | Код |
|----------|-----|
| Запуск стратегии | `HomePage.ToggleBypassAsync` → `StrategyService.StartStrategyAsync` → `ProcessRunner.RunBridgeAsync("StartStrategy")` → `Scripts/ui-bridge.ps1` |
| Остановка | `StrategyService.StopStrategyAsync` — kill процессов `winws` |
| Первый запуск / нет zapret | `App.EnsureZapretInstalled` → `BootstrapWindow` → `ZapretBootstrapService` (GitHub API Flowseal) |
| Дефолты после установки | `ServiceDefaultsService` — TCP+UDP game filter + загрузка IPSet (в фоне, без блокировки UI) |
| Трей | `TrayIconService` + WPF `TrayMenuPopup` (стиль NavButton) |
| Обновление UI | `AppSelfUpdateService` + `ZapretUI-Setup.exe` с GitHub Releases |
| i18n | `LocalizationService` + JSON в `Resources/` |

## Критические баги (уже исправлены — не ломать снова)

1. **Deadlock при старте (v1.6.3):** не вызывать `ApplyFreshInstallDefaults().GetAwaiter().GetResult()` на UI-потоке до `MainWindow.Show()` — только `Task.Run` в фоне
2. **Ложная ошибка winws после Stop (v1.6.4):** отменять `CancellationTokenSource` при остановке во время `StartStrategyAsync`
3. **Двойной запуск bridge (v1.6.4):** в `ProcessRunner.RunBridgeAsync` скрипт вызывался дважды — оставить один вызов
4. **Пустое меню трея (v1.6.6–v1.6.7):** WinForms `ToolStripLabel` + кастомный renderer — заменено на WPF `TrayMenuPopup` (v1.6.8)

## Версионирование и релизы

- Версия в `ZapretUI.csproj` → `<Version>`
- `packaging/update.json` и корневой `update.json` — URL установщика для автообновления
- Сборка: `.\build-installer.ps1` → `ZapretUI-dist\ZapretUI-Setup.exe`
- На GitHub Releases — **только** `ZapretUI-Setup.exe`, один активный тег (старые удаляются)

## UI / стили

- Глобальные стили: `App.xaml` — `NavButton`, `PrimaryButton`, `SecondaryButton`, палитра `#08090d` …
- Фоны главной: `Controls/Backgrounds/`, каталог `HomeBackgroundCatalog`, по умолчанию `wavy`
- Диалоги: `ConfirmWindow`, `MessageWindow` (тематические, не системные MessageBox где возможно)

## Зависимости NuGet

- AvalonEdit — редактор `.bat`
- System.Drawing.Common — иконка трея, генерация иконок

## Типичные запросы пользователя

- Новая стратегия в комплекте → `packaging/strategies/` + `BundledStrategiesService.DeployTo`
- Новый пункт меню / страница → `MainWindow.BuildNavigation`, новый `*Page.xaml.cs`
- Перевод → ключи в `Strings.*.json`
- Поведение трея → `TrayIconService`, `TrayMenuPopup`

## Ссылки

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [FILE_INDEX.md](FILE_INDEX.md)
- [CHANGELOG.md](CHANGELOG.md)
- [USER_GUIDE.md](USER_GUIDE.md)
