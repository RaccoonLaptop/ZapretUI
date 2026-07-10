namespace ZapretUI.Models;

public enum PresetTestKind
{
    Standard,
    DpiFreeze
}

public sealed class TestTargetRow
{
    public required string Name { get; init; }
    public string Http { get; set; } = "…";
    public string Tls12 { get; set; } = "…";
    public string Tls13 { get; set; } = "…";
    public string Ping { get; set; } = "…";
    public bool PingOnly { get; init; }
}

public sealed class PresetScoreRow
{
    public required string FileName { get; init; }
    public required string DisplayName { get; init; }
    public required string Detail { get; init; }
    public required string Glyph { get; init; }
    public int RankScore { get; init; }
    public int Fail { get; init; }
    public int HttpOk { get; init; }
}
