# Архитектура Zapret UI

## Слои

```
┌─────────────────────────────────────────────────────────┐
│  WPF UI: MainWindow, Pages, Windows, TrayMenuPopup      │
├─────────────────────────────────────────────────────────┤
│  Services: Strategy, Paths, Settings, Updates, i18n     │
├─────────────────────────────────────────────────────────┤
│  ProcessRunner + PowerShell Scripts (ui-bridge.ps1)     │
├─────────────────────────────────────────────────────────┤
│  Flowseal zapret: winws.exe, .bat, lists, utils        │
└─────────────────────────────────────────────────────────┘
```

## Точка входа

`App.xaml.cs` → `OnStartup`:

1. Загрузка `AppSettings`, `LocalizationService.Initialize`
2. Режим `--update-progress` → `UpdateInstallProgressWindow`
3. `EnsureZapretInstalled` (bootstrap при необходимости)
4. `BundledStrategiesService.DeployTo`
5. `MainWindow.Show` + фоновый `ServiceDefaultsService`
6. `StartupUpdateService.CheckAndPromptAsync` на `MainWindow.Loaded`

## MainWindow

- Боковая навигация: Главная, Стратегии, Сервис, Консоль (Tools)
- Статус winws раз в секунду (`DispatcherTimer`)
- Сворачивание в трей при закрытии, если обход запущен
- `TrayIconService` — иконка + WPF-меню по ПКМ

## Запуск стратегии (детально)

1. `StrategyService.StartStrategyAsync(batFileName)`
2. Если winws уже есть → `StopStrategyAsync`
3. `ProcessRunner.RunBridgeAsync("StartStrategy", batFileName)`
4. `ui-bridge.ps1` парсит строку `winws.exe` из `.bat`, подставляет `%BIN%`, `%LISTS%`, game filter
5. Запуск `winws` **без видимой консоли** (скрытый процесс)
6. Ожидание до 2 с, проверка `Process.GetProcessesByName("winws")`
7. При отмене (`CancellationToken`) — `OperationCanceledException`, без диалога ошибки

## Сервисная вкладка

- Game filter: файл `utils/game_filter.enabled` (`all` / `tcp` / `udp` / off)
- IPSet: `lists/ipset-all.txt`, обновление через `UpdateService.UpdateIpsetListAsync`
- Язык: `AppSettings.Language` → перезапуск
- Безопасность: `SecuritySetupService` + `SetupWindow`
- Ссылки, обновления Flowseal и Zapret UI

## Обновление приложения

`AppSelfUpdateService` читает `update.json` с GitHub → скачивает `ZapretUI-Setup.exe` → тихая установка через Inno Setup + перезапуск.

## Inno Setup

`installer/ZapretUI.iss` — self-contained publish, иконки из `Assets/`.
