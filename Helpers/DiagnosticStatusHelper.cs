using System.Windows;
using System.Windows.Media;

namespace ZapretUI.Helpers;

public static class DiagnosticStatusHelper
{
    public static string ToDisplayText(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token is "…" or "-")
            return "…";

        var t = token.Trim();
        if (t.Contains("OK", StringComparison.OrdinalIgnoreCase))
            return "OK";
        if (t.Contains("UNSUP", StringComparison.OrdinalIgnoreCase)
            || t.Contains("НЕПОДД", StringComparison.OrdinalIgnoreCase))
            return "N/A";
        if (t.Contains("ERR", StringComparison.OrdinalIgnoreCase)
            || t.Contains("ОШИБКА", StringComparison.OrdinalIgnoreCase)
            || t.Contains("ОШ:", StringComparison.OrdinalIgnoreCase)
            || t.Contains("FAIL", StringComparison.OrdinalIgnoreCase)
            || t.Contains("SSL", StringComparison.OrdinalIgnoreCase))
            return "FAIL";
        if (t.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Таймаут", StringComparison.OrdinalIgnoreCase))
            return "TIMEOUT";

        return t;
    }

    public static Brush ToBrush(string? token)
    {
        var text = ToDisplayText(token);
        return text switch
        {
            "OK" => GetBrush("SuccessBrush"),
            "FAIL" => GetBrush("ErrorBrush"),
            "TIMEOUT" or "N/A" => GetBrush("WarningBrush"),
            _ => GetBrush("TextMutedBrush")
        };
    }

    public static Brush PingBrush(string? ping)
    {
        if (string.IsNullOrWhiteSpace(ping) || ping is "…")
            return GetBrush("TextMutedBrush");

        if (ping.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
            || ping.Contains("Таймаут", StringComparison.OrdinalIgnoreCase))
            return GetBrush("WarningBrush");

        return new SolidColorBrush(Color.FromRgb(0x5C, 0xD4, 0xD4));
    }

    private static Brush GetBrush(string key) =>
        (Brush)Application.Current.FindResource(key);
}
