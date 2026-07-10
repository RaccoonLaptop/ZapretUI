using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using ZapretUI.Models;

namespace ZapretUI.Helpers;

public sealed class TestRunTracker
{
    private readonly StringBuilder _lineBuffer = new();
    private readonly Dictionary<string, TestTargetRow> _targetIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PresetTargetSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _finalizedPresets = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<TestTargetDefinition> _targetTemplate = [];
    private Dictionary<string, string> _templateNames = new(StringComparer.OrdinalIgnoreCase);
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

    public bool TryGetSnapshot(string fileName, out PresetTargetSnapshot snapshot) =>
        _snapshots.TryGetValue(NormalizeBat(fileName), out snapshot!);

    public void BeginRun(PresetTestKind kind, IReadOnlyList<TestTargetDefinition>? targetTemplate = null)
    {
        TestKind = kind;
        Targets.Clear();
        Scores.Clear();
        _targetIndex.Clear();
        _snapshots.Clear();
        _finalizedPresets.Clear();
        _lineBuffer.Clear();
        _currentDpiTarget = null;
        _targetTemplate = targetTemplate ?? [];
        _templateNames = BuildTemplateNameMap(_targetTemplate);
        CurrentPreset = "";
        CurrentPresetDisplay = "";
        BestPresetFile = "";
        Progress = 0;
        ProgressText = "";
        IsRunning = true;
        StatusText = kind == PresetTestKind.DpiFreeze
            ? Loc.T("tools.test_status_running_dpi")
            : Loc.T("tools.test_status_running");

        if (kind == PresetTestKind.Standard && _targetTemplate.Count > 0)
            SeedStandardTargets();

        Notify();
    }

    public void EndRun(int exitCode)
    {
        FlushBuffer();
        FinalizeCurrentPresetIfNeeded();
        IsRunning = false;
        StatusText = exitCode == 0
            ? Loc.T("tools.test_status_done")
            : Loc.F("tools.test_exited", exitCode);
        Notify();
    }

    public void StopRun()
    {
        FlushBuffer();
        FinalizeCurrentPresetIfNeeded();
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
            if (nl < 0)
            {
                TrimCarriageReturnOverwrite();
                break;
            }

            var line = NormalizePhysicalLine(text[..nl]);
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
            FinalizeCurrentPresetIfNeeded();
            CurrentPreset = NormalizeBat(fileName);
            CurrentPresetDisplay = StrategyDisplayHelper.ToDisplayName(CurrentPreset);
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
            TryFinalizeIfTargetsComplete();
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
                    Ping = "n/a",
                    PingOnly = false
                });
                return;
            }

            if (_currentDpiTarget is not null
                && TestRunParser.TryParseDpiStatusLine(plain, out var label, out var status)
                && _targetIndex.TryGetValue(NormalizeTargetKey(_currentDpiTarget), out var dpiRow))
            {
                if (label.Equals("HTTP", StringComparison.OrdinalIgnoreCase))
                    dpiRow.Http = status;
                else if (label.Contains("1.2", StringComparison.Ordinal))
                    dpiRow.Tls12 = status;
                else if (label.Contains("1.3", StringComparison.Ordinal))
                    dpiRow.Tls13 = status;
                Notify();
                TryFinalizeIfTargetsComplete();
                return;
            }
        }

        if (TestRunParser.TryParseScoreRow(plain, out var score))
        {
            UpsertScore(score);
            return;
        }

        if (TestRunParser.TryParseBestConfig(plain, out var best))
        {
            BestPresetFile = NormalizeBat(best.Trim());
            StatusText = Loc.F("tools.test_best_found", StrategyDisplayHelper.ToDisplayName(BestPresetFile));
            Notify();
        }
    }

    private void UpsertTarget(TestTargetRow target)
    {
        var name = TestKind == PresetTestKind.Standard && _targetTemplate.Count > 0
            ? ResolveCanonicalName(target.Name)
            : target.Name.Trim();
        var key = NormalizeTargetKey(name);

        if (TestKind == PresetTestKind.Standard && _targetTemplate.Count > 0)
        {
            if (!_targetIndex.TryGetValue(key, out var seeded))
                return;

            MergeTargetValues(seeded, target);
            Notify();
            return;
        }

        if (_targetIndex.TryGetValue(key, out var existing))
        {
            MergeTargetValues(existing, target);
        }
        else
        {
            var row = new TestTargetRow
            {
                Name = name,
                PingOnly = target.PingOnly,
                Http = target.Http,
                Tls12 = target.Tls12,
                Tls13 = target.Tls13,
                Ping = target.Ping
            };
            Targets.Add(row);
            _targetIndex[key] = row;
        }
        Notify();
    }

    private static void MergeTargetValues(TestTargetRow existing, TestTargetRow target)
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

    private void TryFinalizeIfTargetsComplete()
    {
        if (string.IsNullOrEmpty(CurrentPreset) || _finalizedPresets.Contains(CurrentPreset))
            return;
        if (!AreAllTargetsComplete())
            return;
        FinalizeCurrentPreset();
    }

    private void FinalizeCurrentPresetIfNeeded()
    {
        if (string.IsNullOrEmpty(CurrentPreset) || _finalizedPresets.Contains(CurrentPreset))
            return;
        if (Targets.Count == 0)
            return;
        FinalizeCurrentPreset();
    }

    private void FinalizeCurrentPreset()
    {
        if (string.IsNullOrEmpty(CurrentPreset) || _finalizedPresets.Contains(CurrentPreset))
            return;

        var clones = CloneTargets(TestTargetRowFormatter.DedupeByKey(Targets));
        if (clones.Count == 0)
            return;

        _snapshots[CurrentPreset] = new PresetTargetSnapshot
        {
            FileName = CurrentPreset,
            DisplayName = CurrentPresetDisplay,
            Targets = clones
        };
        _finalizedPresets.Add(CurrentPreset);

        var score = TestKind == PresetTestKind.DpiFreeze
            ? PresetScoreCalculator.FromDpiTargets(CurrentPreset, clones)
            : PresetScoreCalculator.FromStandardTargets(CurrentPreset, clones);
        UpsertScore(score);
    }

    private bool AreAllTargetsComplete()
    {
        if (Targets.Count == 0)
            return false;

        foreach (var row in Targets)
        {
            if (TestKind != PresetTestKind.DpiFreeze && row.Ping is "…")
                return false;

            if (!row.PingOnly)
            {
                if (row.Http is "HTTP:…" or "…")
                    return false;
                if (row.Tls12 is "TLS1.2:…" or "…")
                    return false;
                if (row.Tls13 is "TLS1.3:…" or "…")
                    return false;
            }
        }

        return true;
    }

    private static List<TestTargetRow> CloneTargets(IEnumerable<TestTargetRow> rows) =>
        rows.Select(row => new TestTargetRow
        {
            Name = row.Name,
            PingOnly = row.PingOnly,
            Http = row.Http,
            Tls12 = row.Tls12,
            Tls13 = row.Tls13,
            Ping = row.Ping
        }).ToList();

    private void UpsertScore(PresetScoreRow score)
    {
        var fileName = NormalizeBat(score.FileName);
        var normalized = new PresetScoreRow
        {
            FileName = fileName,
            DisplayName = score.DisplayName,
            Detail = score.Detail,
            Glyph = score.Glyph,
            RankScore = score.RankScore,
            Fail = score.Fail,
            HttpOk = score.HttpOk
        };
        var existingIdx = -1;
        for (var i = 0; i < Scores.Count; i++)
        {
            if (Scores[i].FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                existingIdx = i;
                break;
            }
        }

        if (existingIdx >= 0)
            Scores[existingIdx] = normalized;
        else
            Scores.Add(normalized);

        SortScores();
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
                Http = "HTTP:…",
                Tls12 = "TLS1.2:…",
                Tls13 = "TLS1.3:…",
                Ping = "…"
            };
            Targets.Add(row);
            _targetIndex[NormalizeTargetKey(row.Name)] = row;
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
        ProcessLine(NormalizePhysicalLine(_lineBuffer.ToString()));
        _lineBuffer.Clear();
    }

    private void TrimCarriageReturnOverwrite()
    {
        var text = _lineBuffer.ToString();
        var idx = text.LastIndexOf('\r');
        if (idx < 0) return;

        _lineBuffer.Clear();
        _lineBuffer.Append(text[(idx + 1)..]);
    }

    private static string NormalizePhysicalLine(string line)
    {
        line = line.TrimEnd('\r');
        var idx = line.LastIndexOf('\r');
        if (idx >= 0)
            line = line[(idx + 1)..];
        return line.Trim();
    }

    private void Notify() => Changed?.Invoke();

    private string ResolveCanonicalName(string rawName)
    {
        rawName = rawName.Trim();
        if (string.IsNullOrEmpty(rawName))
            return rawName;

        var leading = Regex.Match(rawName, @"^(\w+)");
        if (!leading.Success)
            return rawName;

        var key = NormalizeTargetKey(leading.Groups[1].Value);
        if (_templateNames.TryGetValue(key, out var templateName))
            return templateName;

        return leading.Groups[1].Value;
    }

    private static Dictionary<string, string> BuildTemplateNameMap(IReadOnlyList<TestTargetDefinition> template)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in template)
            map[NormalizeTargetKey(def.Name)] = def.Name;
        return map;
    }

    private static string NormalizeTargetKey(string name) =>
        name.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

    private static string NormalizeBat(string fileName)
    {
        if (!fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            fileName += ".bat";
        return fileName;
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1B(?:\][^\x07]*\x07|\[[\d;?]*[ -/]*[@-~])", "");
}
