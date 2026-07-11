using System.Text.RegularExpressions;
using ZapretUI.Services;

namespace ZapretUI.Helpers;

/// <summary>
/// Translates ui-bridge RunDiagnostics output when UI language is Russian.
/// </summary>
internal static class DiagnosticOutputLocalizer
{
    private static readonly Dictionary<string, string> ExactLines = new(StringComparer.Ordinal)
    {
        ["[OK] Base Filtering Engine"] = "diag.bfe_ok",
        ["[X] Base Filtering Engine not running"] = "diag.bfe_fail",
        ["[OK] Proxy check passed"] = "diag.proxy_ok",
        ["[OK] TCP timestamps enabled"] = "diag.tcp_ok",
        ["[?] Enabling TCP timestamps..."] = "diag.tcp_enabling",
        ["[X] Adguard found - may cause Discord issues"] = "diag.adguard_fail",
        ["[OK] Adguard check passed"] = "diag.adguard_ok",
        ["[X] Killer services conflict with zapret"] = "diag.killer_fail",
        ["[OK] Killer check passed"] = "diag.killer_ok",
        ["[X] WinDivert64.sys NOT found"] = "diag.windivert_missing",
        ["[OK] WinDivert driver present"] = "diag.windivert_ok",
        ["[OK] No conflicting bypass services"] = "diag.conflicts_ok",
        ["Diagnostics complete"] = "diag.complete",
    };

    private static readonly Regex ProxyEnabled = new(
        @"^\[\?\] System proxy enabled: (.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ConflictingServices = new(
        @"^\[X\] Conflicting services: (.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RemovedService = new(
        @"^  Removed (.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Localize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || LocalizationService.IsEnglish)
            return raw?.TrimEnd() ?? "";

        var lines = raw.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => TranslateLine(ColoredLog.StripColorTag(line).Trim()))
            .Where(line => line.Length > 0);

        return string.Join(Environment.NewLine, lines);
    }

    private static string TranslateLine(string line)
    {
        if (ExactLines.TryGetValue(line, out var key))
            return Loc.T(key);

        var proxy = ProxyEnabled.Match(line);
        if (proxy.Success)
            return Loc.F("diag.proxy_enabled", proxy.Groups[1].Value);

        var conflicts = ConflictingServices.Match(line);
        if (conflicts.Success)
            return Loc.F("diag.conflicts_found", conflicts.Groups[1].Value);

        var removed = RemovedService.Match(line);
        if (removed.Success)
            return Loc.F("diag.service_removed", removed.Groups[1].Value);

        return line;
    }
}
