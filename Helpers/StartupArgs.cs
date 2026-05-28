namespace ZapretUI.Helpers;

public static class StartupArgs
{
    public static bool StartInTray { get; private set; }

    public static void Parse(string[]? args)
    {
        StartInTray = false;
        if (args is null) return;

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase))
                StartInTray = true;
        }
    }
}
