using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class TestStrategiesPage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly EmbeddedTerminalHost _terminal = new();
    private readonly AnsiTerminalRenderer _ansi = new();
    private RichTextBox _output = null!;
    private TextBox _input = null!;
    private Button _startBtn = null!;
    private Button _stopBtn = null!;

    public TestStrategiesPage(ZapretPaths paths)
    {
        _paths = paths;
        BuildUi();
        _terminal.OutputReceived += OnTerminalOutput;
        _terminal.ProcessExited += OnTerminalExited;
        _terminal.ErrorOccurred += OnTerminalError;
        Unloaded += async (_, _) =>
        {
            _terminal.OutputReceived -= OnTerminalOutput;
            _terminal.ProcessExited -= OnTerminalExited;
            _terminal.ErrorOccurred -= OnTerminalError;
            await _terminal.DisposeAsync();
        };
    }

    private void BuildUi()
    {
        var root = new DockPanel();

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(header, Dock.Top);
        header.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test"),
            FontSize = 28,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_subtitle"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });
        root.Children.Add(header);

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(toolbar, Dock.Top);
        _startBtn = MakeButton(Loc.T("tools.test_start"), async () => await StartTestAsync());
        _stopBtn = MakeButton(Loc.T("tools.test_stop"), async () => await StopTestAsync());
        _stopBtn.IsEnabled = false;
        toolbar.Children.Add(_startBtn);
        toolbar.Children.Add(_stopBtn);
        toolbar.Children.Add(MakeButton(Loc.T("tools.clear"), () =>
        {
            _ansi.Clear(_output);
            return Task.CompletedTask;
        }));
        root.Children.Add(toolbar);

        var inputRow = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
        DockPanel.SetDock(inputRow, Dock.Bottom);
        var inputLabel = new TextBlock
        {
            Text = Loc.T("tools.test_input_hint"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
        };
        DockPanel.SetDock(inputLabel, Dock.Top);
        inputRow.Children.Add(inputLabel);

        _input = new TextBox
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            IsEnabled = false,
            Padding = new Thickness(8, 6, 8, 6),
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1)
        };
        _input.KeyDown += async (_, e) =>
        {
            if (!_terminal.IsRunning) return;
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            var line = _input.Text;
            _input.Clear();
            await _terminal.WriteInputAsync(string.IsNullOrEmpty(line) ? "" : line);
        };
        inputRow.Children.Add(_input);
        root.Children.Add(inputRow);

        _output = new RichTextBox
        {
            IsReadOnly = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontFamily = new FontFamily("Consolas, Courier New, Lucida Console"),
            FontSize = 13,
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderThickness = new Thickness(0),
            Document = CreateTerminalDocument()
        };
        TextOptions.SetTextFormattingMode(_output, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(_output, TextRenderingMode.ClearType);
        AnsiTerminalRenderer.ApplyTerminalLayout(_output.Document);

        var border = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = _output
        };
        root.Children.Add(border);

        Content = root;
    }

    private static FlowDocument CreateTerminalDocument()
    {
        var doc = new FlowDocument();
        AnsiTerminalRenderer.ApplyTerminalLayout(doc);
        return doc;
    }

    private static Button MakeButton(string text, Func<Task> action)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 8)
        };
        btn.Click += async (_, _) => await action();
        return btn;
    }

    private async Task StartTestAsync()
    {
        try
        {
            await _terminal.StopAsync();

            var script = ProcessRunner.ResolveTestScript(_paths.Root);
            if (script is null)
            {
                UiHelpers.ShowError(Loc.T("tools.test_script_missing"));
                return;
            }

            TestTargetsService.EnsureTargetsFile(_paths);

            _ansi.Clear(_output);
            AppendLocal($"{Loc.T("tools.test_starting")}{Environment.NewLine}");

            await _terminal.StartPowerShellScriptAsync(script, _paths.Root);

            _startBtn.IsEnabled = false;
            _stopBtn.IsEnabled = true;
            _input.IsEnabled = true;
            _input.Focus();
        }
        catch (Exception ex)
        {
            AppendLocal($"{Loc.T("common.error_prefix")} {ex.Message}{Environment.NewLine}");
            UiHelpers.ShowError(ex.Message);
            SetIdleUi();
        }
    }

    private async Task StopTestAsync()
    {
        await _terminal.StopAsync();
        _ansi.Flush(_output);
        SetIdleUi();
            AppendLocal(Environment.NewLine + Loc.T("tools.test_stopped") + Environment.NewLine);
    }

    private void AppendLocal(string text)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLocal(text));
            return;
        }

        try
        {
            _ansi.Append(_output, text);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void OnTerminalOutput(string chunk) =>
        Dispatcher.InvokeAsync(() =>
        {
            try
            {
                _ansi.Append(_output, chunk);
            }
            catch (Exception ex)
            {
                AppendLocal($"{Loc.T("common.error_prefix")} {ex.Message}{Environment.NewLine}");
            }
        });

    private void OnTerminalError(string message) =>
        Dispatcher.InvokeAsync(() => AppendLocal($"{Loc.T("common.error_prefix")} {message}{Environment.NewLine}"));

    private void OnTerminalExited(int exitCode)
    {
        Dispatcher.Invoke(() =>
        {
            _ansi.Flush(_output);
            SetIdleUi();
            AppendLocal(Environment.NewLine + Loc.F("tools.test_exited", exitCode) + Environment.NewLine);
        });
    }

    private void SetIdleUi()
    {
        _startBtn.IsEnabled = true;
        _stopBtn.IsEnabled = false;
        _input.IsEnabled = false;
    }
}
