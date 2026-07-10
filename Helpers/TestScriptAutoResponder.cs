using System.Text;
using System.Text.RegularExpressions;
using ZapretUI.Models;
using ZapretUI.Services;

namespace ZapretUI.Helpers;

public sealed class TestScriptAutoResponder
{
    private readonly StringBuilder _buffer = new();
    private readonly string _testTypeChoice;
    private readonly EmbeddedTerminalHost _terminal;
    private int _enterPromptCount;

    public TestScriptAutoResponder(PresetTestKind kind, EmbeddedTerminalHost terminal)
    {
        _testTypeChoice = kind == PresetTestKind.DpiFreeze ? "2" : "1";
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

        if (text.Contains("Enter 1 or 2", StringComparison.OrdinalIgnoreCase))
        {
            _enterPromptCount++;
            var answer = _enterPromptCount == 1 ? _testTypeChoice : "1";
            await _terminal.WriteInputAsync(answer);
            _buffer.Clear();
            return;
        }

        if (text.Contains("Press any key", StringComparison.OrdinalIgnoreCase))
        {
            await _terminal.SendKeyAsync(' ');
            _buffer.Clear();
        }
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1B(?:\][^\x07]*\x07|\[[\d;?]*[ -/]*[@-~])", "");
}
