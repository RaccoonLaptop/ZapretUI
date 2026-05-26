using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ZapretUI.Services;

/// <summary>
/// Проверки из community-гайда (issue #8405) — только предупреждения, без изменения конфигов.
/// </summary>
public sealed class SystemHintsService
{
    private readonly string _rootPath;

    public SystemHintsService(ZapretPaths paths) => _rootPath = paths.Root;

    public IReadOnlyList<SystemHint> GetHints()
    {
        var hints = new List<SystemHint>();

        if (HasCyrillicOrSpecialChars(_rootPath))
        {
            hints.Add(new SystemHint(
                HintLevel.Error,
                "Путь к папке содержит кириллицу или спецсимволы",
                "Переместите zapret в простой путь, например C:\\zapret — иначе WinDivert и bat-файлы могут не работать."));
        }

        if (IsProxyEnabled(out var proxy))
        {
            hints.Add(new SystemHint(
                HintLevel.Warning,
                "В системе включён прокси",
                string.IsNullOrWhiteSpace(proxy)
                    ? "Отключите прокси: Win+R → inetcpl.cpl → Подключения → Настройка сети."
                    : $"Прокси: {proxy}. Отключите, если VPN не используется."));
        }

        if (IsAdguardRunning())
        {
            hints.Add(new SystemHint(
                HintLevel.Warning,
                "Запущен AdGuard",
                "Драйвер AdGuard может конфликтовать с Discord при работе zapret."));
        }

        return hints;
    }

    private static bool HasCyrillicOrSpecialChars(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return Regex.IsMatch(path, @"[а-яА-ЯёЁ]") || path.Any(c => " #[]{}^`".Contains(c));
    }

    private static bool IsProxyEnabled(out string? proxyServer)
    {
        proxyServer = null;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            if (key?.GetValue("ProxyEnable") is int enabled && enabled == 1)
            {
                proxyServer = key.GetValue("ProxyServer") as string;
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    private static bool IsAdguardRunning() =>
        System.Diagnostics.Process.GetProcessesByName("AdguardSvc").Length > 0;
}

public enum HintLevel { Info, Warning, Error }

public sealed record SystemHint(HintLevel Level, string Title, string Description);
