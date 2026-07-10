using System.Text.RegularExpressions;
using ZapretUI.Models;

namespace ZapretUI.Helpers;

public static class TestRunParser
{
    private static readonly Regex ConfigHeader = new(
        @"\[\s*(?<cur>\d+)\s*/\s*(?<tot>\d+)\s*\]\s*(?<name>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BestConfig = new(
        @"^Best config:\s*(?<name>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TryParseConfigHeader(string line, out int current, out int total, out string fileName)
    {
        current = 0;
        total = 0;
        fileName = "";
        var m = ConfigHeader.Match(line.Trim());
        if (!m.Success) return false;
        current = int.Parse(m.Groups["cur"].Value);
        total = int.Parse(m.Groups["tot"].Value);
        fileName = m.Groups["name"].Value.Trim();
        return true;
    }

    public static bool TryParseBestConfig(string line, out string fileName)
    {
        fileName = "";
        var m = BestConfig.Match(line.Trim());
        if (!m.Success) return false;
        fileName = m.Groups["name"].Value.Trim();
        return true;
    }

    public static bool TryParseTargetRow(string line, out TestTargetRow row)
    {
        row = null!;
        if (!TestTableLineFormatter.TryParseTestRow(line, out var parsed))
            return false;

        row = new TestTargetRow
        {
            Name = parsed.Name,
            Http = parsed.Http ?? "…",
            Tls12 = parsed.Tls12 ?? "…",
            Tls13 = parsed.Tls13 ?? "…",
            Ping = parsed.Ping,
            PingOnly = parsed.PingOnly
        };
        return true;
    }

    public static bool TryParseScoreRow(string line, out PresetScoreRow row)
    {
        row = null!;
        if (!TestTableLineFormatter.TryParseAnalyticsRow(line, out var parsed))
            return false;

        var httpOk = ExtractInt(parsed.HttpOk);
        var err = ExtractInt(parsed.Err);
        var unsup = ExtractInt(parsed.Unsup);
        var pingOk = ExtractInt(parsed.PingOk);
        var fail = ExtractInt(parsed.Fail);
        var fileName = parsed.Config.Trim();
        if (!fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            fileName += ".bat";

        var glyph = fail == 0 && err == 0 ? "✓" : fail <= 1 ? "≈" : "✗";
        var detail = $"HTTP OK: {httpOk}, ERR: {err}, FAIL: {fail}, Ping OK: {pingOk}, UNSUP: {unsup}";
        var rank = httpOk * 10 - fail * 5 - err * 3 + pingOk;

        row = new PresetScoreRow
        {
            FileName = fileName,
            DisplayName = StrategyDisplayHelper.ToDisplayName(fileName),
            Detail = detail,
            Glyph = glyph,
            HttpOk = httpOk,
            Fail = fail,
            RankScore = rank
        };
        return true;
    }

    private static int ExtractInt(string labeled)
    {
        var m = Regex.Match(labeled, @"(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }
}
