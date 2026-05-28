using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace ZapretUI.Helpers;

/// <summary>
/// Renders PTY output with ANSI colors; emulates \r/\n like a real console.
/// </summary>
public sealed class AnsiTerminalRenderer
{
    private static readonly FontFamily TerminalFont = new("Consolas, Courier New, Lucida Console");
    private const double TerminalFontSize = 13;

    private static readonly Regex CsiSequence = new(
        @"\x1B(?:\][^\x07]*\x07|\[[\d;?]*[ -/]*[@-~])",
        RegexOptions.Compiled);

    private readonly StringBuilder _pending = new();
    private readonly StringBuilder _segmentText = new();
    private readonly List<Run> _lineRuns = new();

    private Brush _foreground;
    private Brush? _background;
    private bool _skipNextBlankLine;
    private readonly TerminalTableBuffer _tableBuffer = new();
    private Paragraph? _liveParagraph;

    public AnsiTerminalRenderer()
    {
        _foreground = GetBrush("TextBrush");
    }

    public void Reset()
    {
        _pending.Clear();
        _segmentText.Clear();
        _lineRuns.Clear();
        _foreground = GetBrush("TextBrush");
        _background = null;
        _skipNextBlankLine = false;
        _tableBuffer.Clear();
        _liveParagraph = null;
    }

    public void AppendFormattedLine(RichTextBox box, IReadOnlyList<(string Text, Brush Foreground)> segments)
    {
        RemoveLiveParagraph(box);
        var p = CreateLineParagraph();
        foreach (var (text, fg) in segments)
            p.Inlines.Add(CreateRun(text, fg));
        box.Document.Blocks.Add(p);
        box.ScrollToEnd();
    }

    public static void ApplyTerminalLayout(FlowDocument document)
    {
        // Prevent soft-wrap like PowerShell: one long line stays on one row (horizontal scroll).
        document.PageWidth = 3200;
        document.IsHyphenationEnabled = false;
        document.IsOptimalParagraphEnabled = false;
        document.TextAlignment = TextAlignment.Left;
        document.Foreground = GetBrush("TextBrush");
        document.FontFamily = TerminalFont;
        document.FontSize = TerminalFontSize;
        document.PagePadding = new Thickness(4);
        XmlAttributeProperties.SetXmlSpace(document, "preserve");

        if (!document.Resources.Contains(typeof(TextElement)))
        {
            var textStyle = new Style(typeof(TextElement));
            textStyle.Setters.Add(new Setter(TextElement.FontFamilyProperty, TerminalFont));
            textStyle.Setters.Add(new Setter(TextElement.FontSizeProperty, TerminalFontSize));
            textStyle.Setters.Add(new Setter(TextElement.FontStyleProperty, FontStyles.Normal));
            textStyle.Setters.Add(new Setter(TextElement.FontWeightProperty, FontWeights.Normal));
            document.Resources.Add(typeof(TextElement), textStyle);
        }
    }

    public void Clear(RichTextBox box)
    {
        Reset();
        box.Document.Blocks.Clear();
        ApplyTerminalLayout(box.Document);
        _liveParagraph = null;
    }

    public void Append(RichTextBox box, string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;
        _pending.Append(chunk);
        ProcessPending(box);
        UpdateLiveLine(box);
    }

    public void Flush(RichTextBox box)
    {
        ProcessPending(box);
        FlushSegment();
        if (_lineRuns.Count > 0 || _segmentText.Length > 0)
            CommitLine(box);
        _tableBuffer.FlushAll(box, this);
    }

    private void ProcessPending(RichTextBox box)
    {
        // PowerShell uses CR+LF; a lone CR was clearing the line buffer before LF — text never appeared.
        var text = _pending.ToString().Replace("\r\n", "\n").Replace("\r", "");
        _pending.Clear();
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] == '\x1B')
            {
                if (i + 1 >= text.Length)
                    break;

                var m = CsiSequence.Match(text, i);
                if (!m.Success || m.Index != i)
                {
                    if (text[i + 1] == '[')
                        break;
                    i++;
                    continue;
                }

                FlushSegment(); // flush text before color / control codes
                HandleCsi(box, m.Value);
                i = m.Index + m.Length;
                continue;
            }

            ProcessChar(box, text[i]);
            i++;
        }

        if (i < text.Length)
            _pending.Append(text[i..]);
    }

    private void ProcessChar(RichTextBox box, char c)
    {
        switch (c)
        {
            case '\n':
                FlushSegment();
                CommitLine(box);
                break;
            default:
                _segmentText.Append(c);
                break;
        }
    }

    /// <summary>Read-Host prompts have no trailing newline until Enter — show them immediately.</summary>
    private void UpdateLiveLine(RichTextBox box)
    {
        if (_segmentText.Length == 0 && _lineRuns.Count == 0)
        {
            RemoveLiveParagraph(box);
            return;
        }

        RemoveLiveParagraph(box);
        _liveParagraph = CreateLineParagraph();

        foreach (var run in _lineRuns)
        {
            var text = run.Text.Replace('\u00A0', ' ');
            _liveParagraph.Inlines.Add(CreateRun(TestOutputLocalizer.TranslateLine(text), run.Foreground));
        }

        if (_segmentText.Length > 0)
        {
            var partial = TestOutputLocalizer.TranslateLine(_segmentText.ToString());
            _liveParagraph.Inlines.Add(CreateRun(partial, _foreground));
        }

        box.Document.Blocks.Add(_liveParagraph);
        box.ScrollToEnd();
    }

    private void RemoveLiveParagraph(RichTextBox box)
    {
        if (_liveParagraph is null) return;
        if (box.Document.Blocks.Contains(_liveParagraph))
            box.Document.Blocks.Remove(_liveParagraph);
        _liveParagraph = null;
    }

    private void HandleCsi(RichTextBox box, string sequence)
    {
        if (sequence.Length < 3 || sequence[1] != '[')
            return;

        // Ignore cursor movement, erase, clear-screen — do not wipe the log (2J made output look empty).
        if (sequence[^1] != 'm')
            return;

        var body = sequence[2..^1];

        ApplySgr(body);
    }

    private void CommitLine(RichTextBox box)
    {
        RemoveLiveParagraph(box);
        FlushSegment();

        var isEmpty = _lineRuns.Count == 0;
        if (isEmpty)
        {
            if (_skipNextBlankLine)
                return;
            _skipNextBlankLine = true;
            _tableBuffer.FlushAll(box, this);
            box.Document.Blocks.Add(CreateLineParagraph());
            box.ScrollToEnd();
            return;
        }

        _skipNextBlankLine = false;
        var plain = PlainTextFromRuns(_lineRuns);

        if (TestTableLineFormatter.ShouldFlushTestTable(plain))
            _tableBuffer.FlushBeforeNonTableLine(box, this);

        if (_tableBuffer.TryBufferLine(plain))
        {
            _lineRuns.Clear();
            return;
        }

        _tableBuffer.FlushBeforeNonTableLine(box, this);

        var p = CreateLineParagraph();
        foreach (var run in _lineRuns)
        {
            var text = run.Text.Replace('\u00A0', ' ');
            run.Text = PreserveTerminalSpaces(TestOutputLocalizer.TranslateLine(text));
            p.Inlines.Add(run);
        }
        _lineRuns.Clear();
        box.Document.Blocks.Add(p);
        box.ScrollToEnd();
    }

    private static string PlainTextFromRuns(List<Run> runs)
    {
        if (runs.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var run in runs)
            sb.Append(run.Text.Replace('\u00A0', ' '));
        return sb.ToString().TrimEnd();
    }

    private Run CreateRun(string text, Brush foreground)
    {
        var run = new Run(PreserveTerminalSpaces(text))
        {
            Foreground = foreground,
            Background = _background,
            FontFamily = TerminalFont,
            FontSize = TerminalFontSize
        };
        XmlAttributeProperties.SetXmlSpace(run, "preserve");
        return run;
    }

    private void FlushSegment()
    {
        if (_segmentText.Length == 0) return;

        // Do not trim spaces — script pads columns with trailing spaces before the next color.
        var text = PreserveTerminalSpaces(_segmentText.ToString().TrimEnd('\r', '\n'));
        _segmentText.Clear();
        if (text.Length == 0) return;

        var run = new Run(text)
        {
            Foreground = _foreground,
            Background = _background,
            FontFamily = TerminalFont,
            FontSize = TerminalFontSize
        };
        XmlAttributeProperties.SetXmlSpace(run, "preserve");
        _lineRuns.Add(run);
    }

    /// <summary>WPF collapses normal spaces between colored Runs; NBSP keeps column padding.</summary>
    private static string PreserveTerminalSpaces(string text) =>
        text.Replace(' ', '\u00A0');

    private static Paragraph CreateLineParagraph()
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0),
            Padding = new Thickness(0),
            LineHeight = TerminalFontSize + 3,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            FontFamily = TerminalFont,
            FontSize = TerminalFontSize
        };
        XmlAttributeProperties.SetXmlSpace(paragraph, "preserve");
        return paragraph;
    }

    private void ApplySgr(string codes)
    {
        if (string.IsNullOrEmpty(codes))
        {
            ResetAttributes();
            return;
        }

        var parts = codes.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (var idx = 0; idx < parts.Length; idx++)
        {
            if (!int.TryParse(parts[idx], out var code)) continue;

            if (code is 38 or 48)
            {
                if (idx + 2 < parts.Length && parts[idx + 1] == "5")
                    idx += 2;
                continue;
            }

            switch (code)
            {
                case 0:
                    ResetAttributes();
                    break;
                case 30:
                    _foreground = PsColor(0);
                    break;
                case 31:
                    _foreground = PsColor(1);
                    break;
                case 32:
                    _foreground = PsColor(2);
                    break;
                case 33:
                    _foreground = PsColor(3);
                    break;
                case 34:
                    _foreground = PsColor(4);
                    break;
                case 35:
                    _foreground = PsColor(5);
                    break;
                case 36:
                    _foreground = PsColor(6);
                    break;
                case 37:
                    _foreground = PsColor(7);
                    break;
                case 39:
                    _foreground = GetBrush("TextBrush");
                    break;
                case 90:
                    _foreground = PsColor(8);
                    break;
                case 91:
                    _foreground = PsColor(9);
                    break;
                case 92:
                    _foreground = PsColor(10);
                    break;
                case 93:
                    _foreground = PsColor(11);
                    break;
                case 94:
                    _foreground = PsColor(12);
                    break;
                case 95:
                    _foreground = PsColor(13);
                    break;
                case 96:
                    _foreground = PsColor(14);
                    break;
                case 97:
                    _foreground = PsColor(15);
                    break;
                case 49:
                    _background = null;
                    break;
            }
        }
    }

    private void ResetAttributes()
    {
        _foreground = GetBrush("TextBrush");
        _background = null;
    }

    private static Brush PsColor(int index) => index switch
    {
        0 => GetBrush("TextMutedBrush"),
        1 => GetBrush("ErrorBrush"),
        2 => GetBrush("SuccessBrush"),
        3 => GetBrush("WarningBrush"),
        4 => GetBrush("AccentBrush"),
        5 => new SolidColorBrush(Color.FromRgb(0xC8, 0x7C, 0xFF)),
        6 => new SolidColorBrush(Color.FromRgb(0x5C, 0xD4, 0xD4)),
        7 => GetBrush("TextBrush"),
        8 => new SolidColorBrush(Color.FromRgb(0x88, 0x90, 0xA8)),
        9 => new SolidColorBrush(Color.FromRgb(0xF0, 0x88, 0x98)),
        10 => new SolidColorBrush(Color.FromRgb(0xA8, 0xE0, 0x70)),
        11 => new SolidColorBrush(Color.FromRgb(0xF0, 0xD0, 0x78)),
        12 => new SolidColorBrush(Color.FromRgb(0x8A, 0xB4, 0xFF)),
        13 => new SolidColorBrush(Color.FromRgb(0xE0, 0xA8, 0xFF)),
        14 => new SolidColorBrush(Color.FromRgb(0x78, 0xE8, 0xE8)),
        15 => new SolidColorBrush(Color.FromRgb(0xF4, 0xF6, 0xFC)),
        _ => GetBrush("TextBrush")
    };

    private static Brush GetBrush(string key) =>
        (Brush)Application.Current.FindResource(key);
}
