using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ZapretUI.Helpers;
using ZapretUI.Models;
using ZapretUI.Services;

namespace ZapretUI;

public sealed class PresetTestRunWindow : Window
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private readonly PresetTestKind _kind;
    private readonly PresetTestScope _scope;
    private readonly IReadOnlyList<string> _batFiles;
    private readonly EmbeddedTerminalHost _terminal = new();
    private readonly AnsiTerminalRenderer _ansi = new();
    private readonly TestRunTracker _tracker = new();
    private TestScriptAutoResponder? _autoResponder;

    private RichTextBox _output = null!;
    private TextBox _input = null!;
    private Button _stopBtn = null!;
    private Button _applyBestBtn = null!;
    private Button _closeBtn = null!;
    private TextBlock _statusText = null!;
    private TextBlock _progressText = null!;
    private ProgressBar _progressBar = null!;
    private TextBlock _currentPresetText = null!;
    private StackPanel _targetsPanel = null!;
    private StackPanel _scoresPanel = null!;
    private Border _logPanel = null!;
    private bool _logVisible;

    public PresetTestRunWindow(
        ZapretPaths paths,
        StrategyService strategy,
        AppSettings settings,
        PresetTestKind kind,
        PresetTestScope scope)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        _kind = kind;
        _scope = scope;
        _batFiles = paths.GetStrategyFiles().ToList();

        Title = Loc.T("tools.test_run_window_title");
        Width = 960;
        Height = 680;
        MinWidth = 760;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("BgBrush");

        BuildUi();
        _tracker.Changed += OnTrackerChanged;
        _terminal.OutputReceived += OnTerminalOutput;
        _terminal.ProcessExited += OnTerminalExited;
        _terminal.ErrorOccurred += OnTerminalError;
        Closed += async (_, _) =>
        {
            _tracker.Changed -= OnTrackerChanged;
            _terminal.OutputReceived -= OnTerminalOutput;
            _terminal.ProcessExited -= OnTerminalExited;
            _terminal.ErrorOccurred -= OnTerminalError;
            await _terminal.DisposeAsync();
        };

        Loaded += async (_, _) => await StartTestAsync();
    }

    private void BuildUi()
    {
        var root = new DockPanel { Margin = new Thickness(18) };

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(header, Dock.Top);
        header.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test"),
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = _kind == PresetTestKind.DpiFreeze
                ? Loc.T("tools.test_mode_dpi_title")
                : Loc.T("tools.test_mode_standard_title"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        root.Children.Add(header);

        var progressCard = CreateCard();
        DockPanel.SetDock(progressCard, Dock.Top);
        progressCard.Margin = new Thickness(0, 0, 0, 12);
        var progressStack = new StackPanel();
        var progressHeader = new Grid();
        progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _statusText = new TextBlock
        {
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap
        };
        _progressText = new TextBlock
        {
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_statusText, 0);
        Grid.SetColumn(_progressText, 1);
        progressHeader.Children.Add(_statusText);
        progressHeader.Children.Add(_progressText);
        progressStack.Children.Add(progressHeader);
        _progressBar = new ProgressBar
        {
            Height = 6,
            Minimum = 0,
            Maximum = 1,
            Margin = new Thickness(0, 8, 0, 0),
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            BorderThickness = new Thickness(0)
        };
        progressStack.Children.Add(_progressBar);
        progressCard.Child = progressStack;
        root.Children.Add(progressCard);

        _logPanel = CreateLogPanel();
        DockPanel.SetDock(_logPanel, Dock.Bottom);
        _logPanel.Visibility = Visibility.Collapsed;
        root.Children.Add(_logPanel);

        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftCard = CreateCard();
        leftCard.Padding = new Thickness(14);
        var leftStack = new DockPanel();
        leftStack.Children.Add(MakeSectionTitle(Loc.T("tools.test_targets_title")));
        _currentPresetText = new TextBlock
        {
            FontWeight = FontWeights.SemiBold,
            FontSize = 13.5,
            Margin = new Thickness(0, 4, 0, 10),
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            TextWrapping = TextWrapping.Wrap,
            Text = Loc.T("tools.test_current_strategy_waiting")
        };
        DockPanel.SetDock(_currentPresetText, Dock.Top);
        leftStack.Children.Add(_currentPresetText);
        var leftScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _targetsPanel = new StackPanel();
        leftScroll.Content = _targetsPanel;
        leftStack.Children.Add(leftScroll);
        leftCard.Child = leftStack;
        Grid.SetColumn(leftCard, 0);
        split.Children.Add(leftCard);

        var rightCard = CreateCard();
        rightCard.Padding = new Thickness(14);
        var rightStack = new DockPanel();
        rightStack.Children.Add(MakeSectionTitle(Loc.T("tools.test_scores_title")));
        rightStack.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_scores_hint"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        var rightScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _scoresPanel = new StackPanel();
        rightScroll.Content = _scoresPanel;
        rightStack.Children.Add(rightScroll);
        rightCard.Child = rightStack;
        Grid.SetColumn(rightCard, 2);
        split.Children.Add(rightCard);

        root.Children.Add(split);

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom);
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftBtns = new WrapPanel();
        _stopBtn = MakeButton(Loc.T("tools.test_stop"), async () => await StopTestAsync());
        _applyBestBtn = MakeButton(Loc.T("tools.test_apply_best"), async () => await ApplyBestAsync());
        _applyBestBtn.IsEnabled = false;
        leftBtns.Children.Add(_stopBtn);
        leftBtns.Children.Add(_applyBestBtn);
        leftBtns.Children.Add(MakeButton(Loc.T("tools.test_toggle_log"), () =>
        {
            _logVisible = !_logVisible;
            _logPanel.Visibility = _logVisible ? Visibility.Visible : Visibility.Collapsed;
            return Task.CompletedTask;
        }));
        Grid.SetColumn(leftBtns, 0);
        footer.Children.Add(leftBtns);

        _closeBtn = new Button
        {
            Content = Loc.T("common.close"),
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 100
        };
        _closeBtn.Click += (_, _) => Close();
        Grid.SetColumn(_closeBtn, 1);
        footer.Children.Add(_closeBtn);

        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        Content = root;
        RefreshVisualState();
    }

    private Border CreateLogPanel()
    {
        _output = new RichTextBox
        {
            IsReadOnly = true,
            Height = 160,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            BorderThickness = new Thickness(0),
            Document = new FlowDocument()
        };
        TextOptions.SetTextFormattingMode(_output, TextFormattingMode.Display);
        AnsiTerminalRenderer.ApplyTerminalLayout(_output.Document);

        _input = new TextBox
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            IsEnabled = false,
            Margin = new Thickness(0, 6, 0, 0)
        };
        MouseWheelScrollHelper.Attach(_input);
        _input.KeyDown += async (_, e) =>
        {
            if (!_terminal.IsRunning || e.Key != Key.Enter) return;
            e.Handled = true;
            var line = _input.Text;
            _input.Clear();
            await _terminal.WriteInputAsync(string.IsNullOrEmpty(line) ? "" : line);
        };

        var stack = new StackPanel();
        stack.Children.Add(_output);
        stack.Children.Add(_input);
        var border = CreateCard();
        border.Child = stack;
        return border;
    }

    private void RefreshVisualState()
    {
        _statusText.Text = _tracker.StatusText;
        _progressText.Text = _tracker.ProgressText;
        _progressBar.Value = _tracker.Progress;

        _currentPresetText.Text = string.IsNullOrWhiteSpace(_tracker.CurrentPresetDisplay)
            ? Loc.T("tools.test_current_strategy_waiting")
            : Loc.F("tools.test_current_strategy", _tracker.CurrentPresetDisplay);

        _applyBestBtn.IsEnabled = !string.IsNullOrWhiteSpace(_tracker.BestPresetFile) && !_tracker.IsRunning;
        _stopBtn.IsEnabled = _tracker.IsRunning;
        _closeBtn.Visibility = _tracker.IsRunning ? Visibility.Collapsed : Visibility.Visible;

        _targetsPanel.Children.Clear();
        if (_tracker.Targets.Count == 0)
        {
            _targetsPanel.Children.Add(MakeMuted(Loc.T("tools.test_targets_empty")));
        }
        else
        {
            var nameWidth = TestTargetRowFormatter.ComputeNameWidth(_tracker.Targets);
            foreach (var row in _tracker.Targets)
            {
                var line = new TextBlock { Margin = new Thickness(0, 0, 0, 4) };
                TestTargetRowFormatter.ApplyRowInlines(line, row, nameWidth);
                _targetsPanel.Children.Add(line);
            }
        }

        _scoresPanel.Children.Clear();
        if (_tracker.Scores.Count == 0)
            _scoresPanel.Children.Add(MakeMuted(Loc.T("tools.test_scores_empty")));
        else
            foreach (var score in _tracker.Scores)
                _scoresPanel.Children.Add(BuildScoreCard(score));
    }

    private Border BuildScoreCard(PresetScoreRow score)
    {
        var card = new Border
        {
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 7)
        };
        var stack = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var glyph = new TextBlock
        {
            Text = score.Glyph,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Width = 18,
            Foreground = score.Glyph switch
            {
                "✓" => (Brush)Application.Current.FindResource("SuccessBrush"),
                "≈" => (Brush)Application.Current.FindResource("WarningBrush"),
                _ => (Brush)Application.Current.FindResource("ErrorBrush")
            }
        };
        var title = new TextBlock
        {
            Text = score.DisplayName,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 0, 8, 0)
        };
        var applyBtn = new Button
        {
            Content = "▶",
            Padding = new Thickness(12, 4, 12, 4),
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            ToolTip = Loc.T("tools.test_apply_preset")
        };
        applyBtn.Click += async (_, _) => await ApplyPresetAsync(score.FileName);
        Grid.SetColumn(glyph, 0);
        Grid.SetColumn(title, 1);
        Grid.SetColumn(applyBtn, 2);
        header.Children.Add(glyph);
        header.Children.Add(title);
        header.Children.Add(applyBtn);
        stack.Children.Add(header);
        stack.Children.Add(new TextBlock
        {
            Text = score.Detail,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11,
            Margin = new Thickness(26, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        card.Child = stack;
        return card;
    }

    private async Task StartTestAsync()
    {
        try
        {
            var script = ProcessRunner.ResolveTestScript(_paths.Root);
            if (script is null)
            {
                UiHelpers.ShowError(Loc.T("tools.test_script_missing"), this);
                Close();
                return;
            }

            TestTargetsService.EnsureTargetsFile(_paths);
            var template = _kind == PresetTestKind.Standard
                ? TestTargetsLoader.LoadDefinitions(_paths)
                : null;

            _ansi.Reset();
            _ansi.Clear(_output);
            _autoResponder = new TestScriptAutoResponder(_kind, _scope, _batFiles, _terminal);
            _autoResponder.Reset();
            _tracker.BeginRun(_kind, template);
            RefreshVisualState();

            await _terminal.StartPowerShellScriptAsync(script, _paths.Root);
            _input.IsEnabled = true;
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message, this);
            _tracker.StopRun();
            RefreshVisualState();
        }
    }

    private async Task StopTestAsync()
    {
        await _terminal.StopAsync();
        _ansi.Flush(_output);
        _tracker.StopRun();
        _input.IsEnabled = false;
        RefreshVisualState();
    }

    private async Task ApplyBestAsync()
    {
        if (string.IsNullOrWhiteSpace(_tracker.BestPresetFile)) return;
        await ApplyPresetAsync(_tracker.BestPresetFile);
    }

    private async Task ApplyPresetAsync(string fileName)
    {
        try
        {
            _settings.LastStrategy = fileName;
            _settings.Save();
            if (_strategy.IsRunning())
                await _strategy.StopStrategyAsync();
            await _strategy.StartStrategyAsync(fileName);
            UiHelpers.ShowInfo(Loc.F("tools.test_applied", StrategyDisplayHelper.ToDisplayName(fileName)), this);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message, this);
        }
    }

    private void OnTerminalOutput(string chunk) =>
        Dispatcher.InvokeAsync(async () =>
        {
            _tracker.Feed(chunk);
            _ansi.Append(_output, chunk);
            if (_autoResponder is not null)
                await _autoResponder.FeedAsync(chunk);
        });

    private void OnTerminalError(string message) =>
        Dispatcher.InvokeAsync(() =>
            _ansi.Append(_output, $"{Loc.T("common.error_prefix")} {message}{Environment.NewLine}"));

    private void OnTerminalExited(int exitCode)
    {
        Dispatcher.Invoke(() =>
        {
            _ansi.Flush(_output);
            _tracker.EndRun(exitCode);
            _input.IsEnabled = false;
            RefreshVisualState();
        });
    }

    private void OnTrackerChanged() => Dispatcher.Invoke(RefreshVisualState);

    private static Border CreateCard() =>
        new()
        {
            Background = (Brush)Application.Current.FindResource("PanelOverlayBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16)
        };

    private static TextBlock MakeSectionTitle(string text) =>
        new()
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 4)
        };

    private static TextBlock MakeMuted(string text) =>
        new()
        {
            Text = text,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };

    private static Button MakeButton(string text, Func<Task> action)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        btn.Click += async (_, _) => await action();
        return btn;
    }
}
