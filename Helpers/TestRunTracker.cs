using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using ZapretUI.Models;

namespace ZapretUI.Helpers;

public sealed class TestRunTracker
{
    private readonly StringBuilder _lineBuffer = new();
    private readonly Dictionary<string, TestTargetRow> _targetIndex = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<TestTargetDefinition> _targetTemplate = [];
    private string? _currentDpiTarget;

    public ObservableCollection<TestTargetRow> Targets { get; } = [];
    public ObservableCollection<PresetScoreRow> Scores { get; } = [];

    public PresetTestKind TestKind { get; private set; } = PresetTestKind.Standard;
    public string CurrentPreset { get; private set; } = "";
    public string CurrentPresetDisplay { get; private set; } = "";
    public string StatusText { get; private set; } = Loc.T("tools.test_status_idle");
    public string BestPresetFile { get; private set; } = "";
    public double Progress { get; private set; }
    public string ProgressText { get; private set; } = "";
    public bool IsRunning { get; private set; }

    public event Action? Changed;

    public void BeginRun(PresetTestKind kind, IReadOnlyList<TestTargetDefinition>? targetTemplate = null)
    {
        TestKind = kind;
        Targets.Clear();
        Scores.Clear();
        _targetIndex.Clear();
        _lineBuffer.Clear();
        _currentDpiTarget = null;
        _targetTemplate = targetTemplate ?? [];
        CurrentPreset = "";
        CurrentPresetDisplay = "";
        BestPresetFile = "";
        Progress = 0;
        ProgressText = "";
        IsRunning = true;
        StatusText = kind == PresetTestKind.DpiFreeze
            ? Loc.T("tools.test_status_running_dpi")
            : Loc.T("tools.test_status_running");
        Notify();
    }

    public void EndRun(int exitCode)
    {
        FlushBuffer();
        IsRunning = false;
        StatusText = exitCode == 0
            ? Loc.T("tools.test_status_done")
            : Loc.F("tools.test_exited", exitCode);
        Notify();
    }

    public void StopRun()
    {
        FlushBuffer();
        IsRunning = false;
        StatusText = Loc.T("tools.test_stopped");
        Notify();
    }

    public void Feed(string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;

        var plain = StripAnsi(chunk);
        _lineBuffer.Append(plain);

        while (true)
        {
            var text = _lineBuffer.ToString();
            var nl = text.IndexOf('\n');
            if (nl < 0) break;

            var line = text[..nl].TrimEnd('\r');
            _lineBuffer.Clear();
            _lineBuffer.Append(text[(nl + 1)..]);
            ProcessLine(line);
        }
    }

    private void ProcessLine(string line)
    {
        var plain = line.Trim();
        if (plain.Length == 0) return;

        if (TestRunParser.TryParseConfigHeader(plain, out var cur, out var tot, out var fileName))
        {
            CurrentPreset = fileName;
            CurrentPresetDisplay = StrategyDisplayHelper.ToDisplayName(fileName);
            _currentDpiTarget = null;
            if (TestKind == PresetTestKind.Standard && _targetTemplate.Count > 0)
                SeedStandardTargets();
            else
            {
                Targets.Clear();
                _targetIndex.Clear();
            }
            if (tot > 0)
            {
                Progress = (double)cur / tot;
                ProgressText = $"{cur} / {tot}";
            }
            StatusText = Loc.F("tools.test_status_preset", CurrentPresetDisplay, cur, tot);
            Notify();
            return;
        }

        if (TestKind == PresetTestKind.Standard && TestRunParser.TryParseTargetRow(plain, out var target))
        {
            UpsertTarget(target);
            return;
        }

        if (TestKind == PresetTestKind.DpiFreeze)
        {
            if (TestRunParser.TryParseDpiTargetHeader(plain, out var dpiName))
            {
                _currentDpiTarget = dpiName;
                UpsertTarget(new TestTargetRow
                {
                    Name = dpiName,
                    Http = "…",
                    Tls12 = "…",
                    Tls13 = "…",
                    PingOnly = false
                });
                return;
            }

            if (_currentDpiTarget is not null
                && TestRunParser.TryParseDpiStatusLine(plain, out var label, out var status)
                && _targetIndex.TryGetValue(_currentDpiTarget, out var dpiRow))
            {
                if (label.Equals("HTTP", StringComparison.OrdinalIgnoreCase))
                    dpiRow.Http = status;
                else if (label.Contains("1.2", StringComparison.Ordinal))
                    dpiRow.Tls12 = status;
                else if (label.Contains("1.3", StringComparison.Ordinal))
                    dpiRow.Tls13 = status;
                Notify();
                return;
            }
        }

        if (TestRunParser.TryParseScoreRow(plain, out var score))
        {
            var existingIdx = -1;
            for (var i = 0; i < Scores.Count; i++)
            {
                if (Scores[i].FileName.Equals(score.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    existingIdx = i;
                    break;
                }
            }

            if (existingIdx >= 0)
                Scores[existingIdx] = score;
            else
                Scores.Add(score);

            SortScores();
            Notify();
            return;
        }

        if (TestRunParser.TryParseBestConfig(plain, out var best))
        {
            BestPresetFile = best.Trim();
            StatusText = Loc.F("tools.test_best_found", StrategyDisplayHelper.ToDisplayName(BestPresetFile));
            Notify();
        }
    }

    private void UpsertTarget(TestTargetRow target)
    {
        if (_targetIndex.TryGetValue(target.Name, out var existing))
        {
            if (!target.PingOnly)
            {
                if (target.Http is not ("…" or "HTTP:…"))
                    existing.Http = target.Http;
                if (target.Tls12 is not ("…" or "TLS1.2:…"))
                    existing.Tls12 = target.Tls12;
                if (target.Tls13 is not ("…" or "TLS1.3:…"))
                    existing.Tls13 = target.Tls13;
            }
            if (target.Ping is not "…")
                existing.Ping = target.Ping;
        }
        else
        {
            Targets.Add(target);
            _targetIndex[target.Name] = target;
        }
        Notify();
    }

    private void SeedStandardTargets()
    {
        Targets.Clear();
        _targetIndex.Clear();
        foreach (var def in _targetTemplate)
        {
            var row = new TestTargetRow
            {
                Name = def.Name,
                PingOnly = def.PingOnly,
                Http = def.PingOnly ? "HTTP:…" : "HTTP:…",
                Tls12 = "TLS1.2:…",
                Tls13 = "TLS1.3:…",
                Ping = "…"
            };
            Targets.Add(row);
            _targetIndex[row.Name] = row;
        }
        Notify();
    }

    private void SortScores()
    {
        var ordered = Scores.OrderByDescending(s => s.RankScore)
            .ThenBy(s => s.Fail)
            .ThenByDescending(s => s.HttpOk)
            .ToList();
        Scores.Clear();
        foreach (var s in ordered)
            Scores.Add(s);
    }

    private void FlushBuffer()
    {
        if (_lineBuffer.Length == 0) return;
        ProcessLine(_lineBuffer.ToString());
        _lineBuffer.Clear();
    }

    private void Notify() => Changed?.Invoke();

    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1B(?:\][^\x07]*\x07|\[[\d;?]*[ -/]*[@-~])", "");
}
