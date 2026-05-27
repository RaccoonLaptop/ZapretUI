using System.IO;
using System.Windows;
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
                    "Установить обновление сейчас?"))
            {
                var install = await appUpdater.InstallUpdateAsync(appCheck.Manifest!);
                ConsoleLog.Instance.Write(install.Message);

                if (install.RequiresRestart)
                    return true;

                if (!install.Success)
                    UiHelpers.ShowError(install.Message);
            }
        }

        if (flowUpdateAvailable)
        {
            if (UiHelpers.Confirm(
                    $"Доступна новая версия Flowseal/zapret-discord-youtube.\n\n" +
                    $"Новая: {flowCheck.RemoteVersion}\n" +
                    $"У вас: {flowCheck.LocalVersion}\n\n" +
                    "Переустановить компоненты zapret из GitHub?\n" +
                    "Ваши правки в .bat и lists могут быть заменены."))
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
