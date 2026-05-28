using Microsoft.Win32;

namespace ZapretUI.Services;

public static class AppStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ZapretUI";

    public static bool IsEnabled()
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

    public static void Enable()
    {
        var exe = ResolveExecutablePath();
        if (exe is null) return;

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        key?.SetValue(ValueName, $"\"{exe}\" --tray");
    }

    public static void Disable()
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

    public static string? ResolveExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        path = Path.Combine(AppContext.BaseDirectory, "ZapretUI.exe");
        return File.Exists(path) ? path : null;
    }
}
