using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZapretUI.Models;

public sealed class TestTargetRow : INotifyPropertyChanged
{
    private string _http = "…";
    private string _tls12 = "…";
    private string _tls13 = "…";
    private string _ping = "…";

    public required string Name { get; init; }

    public string Http
    {
        get => _http;
        set { _http = value; OnPropertyChanged(); }
    }

    public string Tls12
    {
        get => _tls12;
        set { _tls12 = value; OnPropertyChanged(); }
    }

    public string Tls13
    {
        get => _tls13;
        set { _tls13 = value; OnPropertyChanged(); }
    }

    public string Ping
    {
        get => _ping;
        set { _ping = value; OnPropertyChanged(); }
    }

    public bool PingOnly { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class PresetScoreRow
{
    public required string FileName { get; init; }
    public required string DisplayName { get; init; }
    public required string Detail { get; init; }
    public required string Glyph { get; init; }
    public int HttpOk { get; init; }
    public int Fail { get; init; }
    public int RankScore { get; init; }
}
