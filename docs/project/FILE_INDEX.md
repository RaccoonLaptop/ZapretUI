# Индекс исходных файлов

Корень репозитория = папка проекта. Пути относительные к корню.

## Приложение (WPF)

| Файл | Назначение |
|------|------------|
| `App.xaml` / `App.xaml.cs` | Старт, bootstrap, дефолты, глобальные стили |
| `MainWindow.xaml` / `.cs` | Оболочка, навигация, статус, трей |
| `TrayMenuPopup.xaml` / `.cs` | WPF-меню в системном трее |
| `BootstrapWindow.cs` | Окно первой загрузки Flowseal |
| `SetupWindow.xaml.cs` | Мастер антивируса/брандмауэра |
| `ToolsWindow.cs` | Консоль логов |
| `StrategyListsWindow.cs` | Редактор list-файлов стратегии |
| `UpdateDownloadWindow.cs` | Прогресс скачивания обновления |
| `UpdateInstallProgressWindow.cs` | Прогресс установки обновления |

## Страницы

| Файл | Назначение |
|------|------------|
| `Pages/HomePage.xaml.cs` | Главная: выбор стратегии, Запустить/Остановить |
| `Pages/StrategiesPage.xaml.cs` | Список и редактор .bat (AvalonEdit) |
| `Pages/ServicePage.xaml.cs` | Сервис, фильтры, язык, обновления |

## Сервисы (`Services/`)

| Файл | Назначение |
|------|------------|
| `StrategyService.cs` | Start/Stop winws, чтение/запись .bat |
| `ProcessRunner.cs` | PowerShell, ui-bridge, тесты |
| `ZapretPaths.cs` | Пути к zapret, валидация корня |
| `ZapretBootstrapService.cs` | Скачивание Flowseal с GitHub |
| `ServiceDefaultsService.cs` | Дефолты после установки |
| `ServiceSettingsService.cs` | Game filter, IPSet status |
| `AppSettings.cs` | settings.json |
| `LocalizationService.cs` | i18n JSON |
| `UpdateService.cs` | Версия/IPSet Flowseal |
| `AppSelfUpdateService.cs` | Обновление Zapret UI |
| `StartupUpdateService.cs` | Проверка при старте |
| `TrayIconService.cs` | NotifyIcon + popup |
| `BundledStrategiesService.cs` | Встроенные стратегии |
| `SecuritySetupService.cs` | Defender + firewall |
| `NetworkResetService.cs` | Сброс сети |
| `ConsoleLog.cs` | Лог для ToolsWindow |

## Хелперы (`Helpers/`)

| Файл | Назначение |
|------|------------|
| `Loc.cs` | `Loc.T` / `Loc.F` |
| `UiHelpers.cs` | Диалоги, навигационные кнопки |
| `AppIcon.cs` | Иконка окна и панели задач |
| `TrayIconGenerator.cs` | Иконка трея (синяя/зелёная) |
| `ConfirmWindow.cs` / `MessageWindow.cs` | Тематические диалоги |
| `BatchSyntaxHighlighting.cs` | Подсветка .bat |
| `FindReplaceDialog.cs` | Поиск в редакторе |

## Фоны (`Controls/Backgrounds/`)

| Файл | Назначение |
|------|------------|
| `AnimatedBackgroundBase.cs` | Базовый цикл анимации |
| `HomeBackgroundCatalog.cs` | Список фонов, default `wavy` |
| `HomeBackgroundVariants.cs` | Meteors, Aurora, Wavy, … |
| `ShootingStarsBackground.cs` | Звёзды |
| `HomeBackgroundHost.cs` | Контейнер на главной |

## Скрипты (`Scripts/`)

| Файл | Назначение |
|------|------------|
| `ui-bridge.ps1` | StartStrategy, InstallService, тесты |
| `bootstrap-zapret.ps1` | Резервный bootstrap |
| `apply-update-installer.ps1` | Установка обновления |
| `security-setup.ps1` | Исключения Defender |
| `stop-zapret-components.ps1` | Остановка при удалении |

## Ресурсы и сборка

| Файл | Назначение |
|------|------------|
| `Resources/Strings.ru.json` | Русские строки |
| `Resources/Strings.en.json` | English strings |
| `packaging/strategies/Niko_ALT11.*` | Встроенная стратегия |
| `packaging/update.json` | Манифест автообновления |
| `installer/ZapretUI.iss` | Inno Setup |
| `build-installer.ps1` | Полная сборка установщика |
| `build-icon.ps1` | Генерация app.ico |

## Документация (`docs/`)

| Файл | Назначение |
|------|------------|
| `docs/index.html` | Сайт-руководство (GitHub Pages) |
| `docs/project/*` | Markdown для агентов и разработчиков |
| `AGENTS.md` | Точка входа для Cursor |
