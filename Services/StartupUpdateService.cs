using System.IO;
using System.Windows;
using ZapretUI;
using ZapretUI.Helpers;

namespace ZapretUI.Services;

public sealed class StartupUpdateService
{
    public async Task<bool> CheckAndPromptAsync(Window owner, AppSettings settings, ZapretPaths paths)
    {
        if (!settings.CheckUpdatesOnStartup)
            return false;

        var appUpdater = new AppSelfUpdateService(settings, paths.Root);
        var flowUpdater = new UpdateService(paths);

        var appCheckTask = appUpdater.CheckForUpdateAsync();
        var flowCheckTask = flowUpdater.CheckForUpdatesAsync();
        await Task.WhenAll(appCheckTask, flowCheckTask);

        var appCheck = await appCheckTask;
        var flowCheck = await flowCheckTask;

        ConsoleLog.Instance.Write(
            $"Проверка версий при запуске: Zapret UI {appCheck.LocalVersion}, Flowseal {flowCheck.LocalVersion}");

        if (appCheck.Error is not null)
            ConsoleLog.Instance.Write($"Zapret UI: {appCheck.Error}");
        if (flowCheck.Error is not null)
            ConsoleLog.Instance.Write($"Flowseal: {flowCheck.Error}");

        var appUpdateAvailable = appCheck.HasUpdate && appCheck.Manifest is not null;
        var flowUpdateAvailable = flowCheck.Error is null && !flowCheck.IsUpToDate;

        if (!appUpdateAvailable && !flowUpdateAvailable)
        {
            ConsoleLog.Instance.Write("Все компоненты актуальны");
            return false;
        }

        if (appUpdateAvailable)
        {
            if (UiHelpers.Confirm(
                    $"Доступна новая версия Zapret UI.\n\n" +
                    $"Новая: {appCheck.RemoteVersion}\n" +
                    $"У вас: {appCheck.LocalVersion}\n\n" +
                    "Скачать обновление сейчас?",
                    owner))
            {
                PreparedAppUpdate? prepared = null;
                var keepPrepared = false;
                UpdateDownloadWindow? progressWin = new UpdateDownloadWindow("Обновление Zapret UI", "Загрузка пакета обновления...");
                progressWin.Owner = owner;
                progressWin.Show();
                try
                {
                    var downloadProgress = new Progress<DownloadProgress>(p => progressWin.ReportProgress(p));
                    var preparedResult = await appUpdater.PrepareUpdateAsync(
                        appCheck.Manifest!, downloadProgress);
                    if (!preparedResult.Success || preparedResult.Payload is null)
                    {
                        UiHelpers.ShowError(preparedResult.Message);
                    }
                    else
                    {
                        prepared = preparedResult.Payload;
                        progressWin.SetStatus("Загрузка завершена. Пакет готов к установке.");
                        if (UiHelpers.Confirm(
                                $"Пакет обновления Zapret UI {appCheck.RemoteVersion} загружен.\n\nУстановить сейчас?",
                                owner))
                        {
                            progressWin.SetStatus("Запуск установки…");
                            progressWin.ReportProgress(new DownloadProgress
                            {
                                Phase = "Сейчас откроется окно обновления"
                            });
                            var install = await appUpdater.InstallPreparedUpdateAsync(prepared);
                            progressWin.Close();
                            progressWin = null!;
                            ConsoleLog.Instance.Write(install.Message);
                            keepPrepared = install.KeepPreparedFiles;
                            if (install.RequiresRestart)
                                return true;
                            if (!install.Success)
                                UiHelpers.ShowError(install.Message);
                        }
                    }
                }
                finally
                {
                    progressWin?.Close();
                    AppSelfUpdateService.CleanupPreparedUpdate(prepared, keepPrepared);
                }
            }
        }

        if (flowUpdateAvailable)
        {
            if (UiHelpers.Confirm(
                    $"Доступна новая версия Flowseal/zapret-discord-youtube.\n\n" +
                    $"Новая: {flowCheck.RemoteVersion}\n" +
                    $"У вас: {flowCheck.LocalVersion}\n\n" +
                    "Переустановить компоненты zapret из GitHub?\n" +
                    "Ваши правки в .bat и lists могут быть заменены.",
                    owner))
            {
                await FlowsealReinstallService.ReinstallAsync(owner, paths);
            }
        }

        return false;
    }
}

public static class FlowsealReinstallService
{
    public static Task<bool> ReinstallAsync(Window? owner, ZapretPaths paths)
    {
        var target = paths.Root;
        try
        {
            if (Directory.Exists(target))
                Directory.Delete(target, true);

            var bootstrap = new BootstrapWindow(target) { Owner = owner };
            if (bootstrap.ShowDialog() != true)
            {
                UiHelpers.ShowResult(owner, "Flowseal", "Установка отменена или не удалась.");
                return Task.FromResult(false);
            }

            UiHelpers.ShowResult(owner, "Flowseal",
                "Компоненты Flowseal переустановлены.\nПерезапустите Zapret UI для применения изменений.");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowResult(owner, "Flowseal", $"Ошибка: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
