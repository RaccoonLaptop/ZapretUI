using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace ZapretUI.Services;

public static class AppStartupService
{
    private const string TaskName = "ZapretUI_Autostart";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ZapretUI";

    public static bool IsEnabled()
    {
        if (IsTaskScheduled())
            return true;

        return IsLegacyRegistryEnabled();
    }

    public static void Enable()
    {
        var exe = ResolveExecutablePath();
        if (exe is null) return;

        if (TryEnableTaskScheduler(exe))
        {
            RemoveLegacyRegistryRun();
            return;
        }

        EnableLegacyRegistryRun(exe);
    }

    public static void Disable()
    {
        RemoveTaskScheduler();
        RemoveLegacyRegistryRun();
    }

    /// <summary>Синхронизирует автозапуск UI с настройкой StartUiOnLogin.</summary>
    public static void SyncWithSettings(bool startOnLogin)
    {
        if (startOnLogin)
            Enable();
        else
            Disable();
    }

    public static string? ResolveExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        path = Path.Combine(AppContext.BaseDirectory, "ZapretUI.exe");
        return File.Exists(path) ? path : null;
    }

    private static bool IsTaskScheduled()
    {
        try
        {
            using var taskService = new TaskService();
            return taskService.GetTask(TaskName) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryEnableTaskScheduler(string exePath)
    {
        try
        {
            using var taskService = new TaskService();
            taskService.RootFolder.DeleteTask(TaskName, false);

            var definition = taskService.NewTask();
            definition.RegistrationInfo.Description =
                "Zapret UI — запуск в трее при входе в Windows (с правами администратора).";
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            var trigger = new LogonTrigger
            {
                UserId = WindowsIdentity.GetCurrent().Name
            };
            definition.Triggers.Add(trigger);

            var workDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            definition.Actions.Add(new ExecAction(exePath, "--tray", workDir));

            definition.Principal.RunLevel = TaskRunLevel.Highest;
            definition.Principal.UserId = WindowsIdentity.GetCurrent().Name;
            definition.Principal.LogonType = TaskLogonType.InteractiveToken;

            taskService.RootFolder.RegisterTaskDefinition(TaskName, definition);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveTaskScheduler()
    {
        try
        {
            using var taskService = new TaskService();
            taskService.RootFolder.DeleteTask(TaskName, false);
        }
        catch
        {
            /* not registered */
        }
    }

    private static bool IsLegacyRegistryEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    private static void EnableLegacyRegistryRun(string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        key?.SetValue(ValueName, $"\"{exePath}\" --tray");
    }

    private static void RemoveLegacyRegistryRun()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.DeleteValue(ValueName, false);
        }
        catch
        {
            /* not registered */
        }
    }
}
