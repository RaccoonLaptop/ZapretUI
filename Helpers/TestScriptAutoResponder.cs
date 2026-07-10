using System.Text;
using System.Text.RegularExpressions;
using ZapretUI.Models;
using ZapretUI.Services;

namespace ZapretUI.Helpers;

public sealed class TestScriptAutoResponder
{
    private readonly StringBuilder _buffer = new();
    private readonly string _testTypeChoice;
    private readonly PresetTestScope _scope;
    private readonly IReadOnlyList<string> _batFiles;
    private readonly EmbeddedTerminalHost _terminal;
    private int _enterPromptCount;

    public TestScriptAutoResponder(
        PresetTestKind kind,
        PresetTestScope scope,
        IReadOnlyList<string> batFiles,
        EmbeddedTerminalHost terminal)
    {
        _testTypeChoice = kind == PresetTestKind.DpiFreeze ? "2" : "1";
        _scope = scope;
        _batFiles = batFiles;
        _terminal = terminal;
    }

    public void Reset()
    {
        _buffer.Clear();
        _enterPromptCount = 0;
    }

    public async Task FeedAsync(string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;

        _buffer.Append(StripAnsi(chunk));
        if (_buffer.Length > 8000)
            _buffer.Remove(0, _buffer.Length - 4000);

        var text = _buffer.ToString();

        if (ContainsSelectionPrompt(text))
        {
            await _terminal.WriteInputAsync(ResolveSelectionAnswer());
            _buffer.Clear();
            return;
        }

        if (text.Contains("Enter 1 or 2", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Введите 1 или 2", StringComparison.OrdinalIgnoreCase))
        {
            _enterPromptCount++;
            var answer = _enterPromptCount switch
            {
                1 => _testTypeChoice,
                2 => _scope.TestAll ? "1" : "2",
                _ => "1"
            };
            await _terminal.WriteInputAsync(answer);
            _buffer.Clear();
            return;
        }

        if (text.Contains("Press any key", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Нажмите любую клавишу", StringComparison.OrdinalIgnoreCase))
        {
            await _terminal.SendKeyAsync(' ');
            _buffer.Clear();
        }
    }

    private static bool ContainsSelectionPrompt(string text) =>
        text.Contains("Enter numbers", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Введите номера", StringComparison.OrdinalIgnoreCase);

    private string ResolveSelectionAnswer()
    {
        if (_scope.TestAll || _scope.SelectedStrategyFiles.Count == 0)
            return "0";

        var indices = new List<int>();
        foreach (var file in _scope.SelectedStrategyFiles)
        {
            for (var i = 0; i < _batFiles.Count; i++)
            {
                if (_batFiles[i].Equals(file, StringComparison.OrdinalIgnoreCase))
                    indices.Add(i + 1);
            }
        }

        if (indices.Count == 0)
            return "1";

        indices.Sort();
        return string.Join(",", indices);
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1B(?:\][^\x07]*\x07|\[[\d;?]*[ -/]*[@-~])", "");
}
