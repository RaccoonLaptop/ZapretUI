using System.Diagnostics;
using System.ServiceProcess;

namespace ZapretUI.Services;

public static class ZapretShutdownService
{
    private static readonly string[] ServiceNames = ["zapret", "WinDivert", "WinDivert14"];

    public static void StopAll()
    {
        foreach (var proc in Process.GetProcessesByName("winws"))
        {
            try { proc.Kill(true); } catch { /* ignore */ }
        }

        foreach (var name in ServiceNames)
        {
            try
            {
                using var svc = new ServiceController(name);
                if (svc.Status == ServiceControllerStatus.Running)
                    svc.Stop();
            }
            catch { /* service may not exist */ }
        }
    }

    public static bool IsWinDivertOrWinwsActive()
    {
        if (Process.GetProcessesByName("winws").Length > 0)
            return true;

        foreach (var name in ServiceNames)
        {
            try
            {
                using var svc = new ServiceController(name);
                if (svc.Status == ServiceControllerStatus.Running)
                    return true;
            }
            catch { /* ignore */ }
        }

        return false;
    }
}
