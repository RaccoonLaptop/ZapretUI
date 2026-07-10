using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ZapretUI.Helpers;
using ZapretUI.Models;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class TestStrategiesPage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private readonly EmbeddedTerminalHost _terminal = new();
    private readonly AnsiTerminalRenderer _ansi = new();
    private readonly TestRunTracker _tracker = new();
    private TestScriptAutoResponder? _autoResponder;

    private RichTextBox _output = null!;
    private TextBox _input = null!;
    private Button _startBtn = null!;
    private Button _stopBtn = null!;
    private Button _applyBestBtn = null!;
    private Border _modeStandard = null!;
    private Border _modeDpi = null!;
    private TextBlock _statusText = null!;
    private TextBlock _progressText = null!;
    private ProgressBar _progressBar = null!;
    private TextBlock _currentPresetText = null!;
    private StackPanel _targetsPanel = null!;
    private StackPanel _scoresPanel = null!;
    private Border _logPanel = null!;
    private PresetTestKind _selectedKind = PresetTestKind.Standard;
    private bool _logVisible;

    public TestStrategiesPage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        BuildUi();
        _tracker.Changed += OnTrackerChanged;
        _terminal.OutputReceived += OnTerminalOutput;
        _terminal.ProcessExited += OnTerminalExited;
        _terminal.ErrorOccurred += OnTerminalError;
        Unloaded += OnUnloadedAsync;
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
            Text = Loc.T("tools.test_subtitle_visual"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });
        root.Children.Add(header);

        var modeRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(modeRow, Dock.Top);
        modeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        modeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        modeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _modeStandard = CreateModeCard(Loc.T("tools.test_mode_standard_title"), Loc.T("tools.test_mode_standard_hint"), true);
        _modeStandard.MouseLeftButtonUp += (_, _) => SelectMode(PresetTestKind.Standard);
        Grid.SetColumn(_modeStandard, 0);
        modeRow.Children.Add(_modeStandard);
        _modeDpi = CreateModeCard(Loc.T("tools.test_mode_dpi_title"), Loc.T("tools.test_mode_dpi_hint"), false);
        _modeDpi.MouseLeftButtonUp += (_, _) => SelectMode(PresetTestKind.DpiFreeze);
        Grid.SetColumn(_modeDpi, 2);
        modeRow.Children.Add(_modeDpi);
        root.Children.Add(modeRow);

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        DockPanel.SetDock(toolbar, Dock.Top);
        _startBtn = MakeButton(Loc.T("tools.test_start"), async () => await StartTestAsync());
        _stopBtn = MakeButton(Loc.T("tools.test_stop"), async () => await StopTestAsync());
        _stopBtn.IsEnabled = false;
        _applyBestBtn = MakeButton(Loc.T("tools.test_apply_best"), async () => await ApplyBestAsync());
        _applyBestBtn.IsEnabled = false;
        toolbar.Children.Add(_startBtn);
        toolbar.Children.Add(_stopBtn);
        toolbar.Children.Add(_applyBestBtn);
        toolbar.Children.Add(MakeButton(Loc.T("tools.test_toggle_log"), () =>
        {
            _logVisible = !_logVisible;
            _logPanel.Visibility = _logVisible ? Visibility.Visible : Visibility.Collapsed;
            return Task.CompletedTask;
        }));
        root.Children.Add(toolbar);

        var progressCard = CreateCard();
        DockPanel.SetDock(progressCard, Dock.Top);
        progressCard.Margin = new Thickness(0, 0, 0, 12);
        var progressStack = new StackPanel();
        var progressHeader = new Grid();
        progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _statusText = new TextBlock
        {
            Text = Loc.T("tools.test_status_idle"),
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
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });

        var leftCard = CreateCard();
        leftCard.Padding = new Thickness(14);
        var leftStack = new DockPanel();
        leftStack.Children.Add(MakeSectionTitle(Loc.T("tools.test_targets_title")));
        _currentPresetText = new TextBlock
        {
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 4, 0, 10),
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            Text = "—"
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
        var scoresHint = new TextBlock
        {
            Text = Loc.T("tools.test_scores_hint"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(scoresHint, Dock.Top);
        rightStack.Children.Add(scoresHint);
        var rightScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _scoresPanel = new StackPanel();
        rightScroll.Content = _scoresPanel;
        rightStack.Children.Add(rightScroll);
        rightCard.Child = rightStack;
        Grid.SetColumn(rightCard, 2);
        split.Children.Add(rightCard);

        root.Children.Add(split);
        Content = root;
        SelectMode(PresetTestKind.Standard);
        RefreshVisualState();
    }

    private Border CreateModeCard(string title, string hint, bool selected)
    {
        var card = new Border
        {
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Cursor = Cursors.Hand
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14
        });
        stack.Children.Add(new TextBlock
        {
            Text = hint,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        card.Child = stack;
        SetModeSelected(card, selected);
        return card;
    }

    private void SelectMode(PresetTestKind kind)
    {
        if (_tracker.IsRunning) return;
        _selectedKind = kind;
        SetModeSelected(_modeStandard, kind == PresetTestKind.Standard);
        SetModeSelected(_modeDpi, kind == PresetTestKind.DpiFreeze);
    }

    private static void SetModeSelected(Border card, bool selected)
    {
        card.BorderBrush = (Brush)Application.Current.FindResource(selected ? "AccentBrush" : "BorderBrush");
        card.BorderThickness = new Thickness(selected ? 2 : 1);
        card.Background = (Brush)Application.Current.FindResource(selected ? "PanelOverlayBrush" : "InputBrush");
    }

    private Border CreateLogPanel()
    {
        _output = new RichTextBox
        {
            IsReadOnly = true,
            Height = 180,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas, Courier New, Lucida Console"),
            FontSize = 12,
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderThickness = new Thickness(0),
            Document = CreateTerminalDocument()
        };
        TextOptions.SetTextFormattingMode(_output, TextFormattingMode.Display);

        _input = new TextBox
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            IsEnabled = false,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(8, 6, 8, 6),
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1)
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

        var stack = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        stack.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_input_hint"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 4)
        });
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
            ? "—"
            : _tracker.CurrentPresetDisplay;
        _applyBestBtn.IsEnabled = !string.IsNullOrWhiteSpace(_tracker.BestPresetFile) && !_tracker.IsRunning;
        _modeStandard.IsEnabled = !_tracker.IsRunning;
        _modeDpi.IsEnabled = !_tracker.IsRunning;

        _targetsPanel.Children.Clear();
        if (_tracker.Targets.Count == 0)
            _targetsPanel.Children.Add(MakeMuted(Loc.T("tools.test_targets_empty")));
        else
            foreach (var row in _tracker.Targets)
                _targetsPanel.Children.Add(BuildTargetCard(row));

        _scoresPanel.Children.Clear();
        if (_tracker.Scores.Count == 0)
            _scoresPanel.Children.Add(MakeMuted(Loc.T("tools.test_scores_empty")));
        else
            foreach (var score in _tracker.Scores)
                _scoresPanel.Children.Add(BuildScoreCard(score));
    }

    private Border BuildTargetCard(TestTargetRow row)
    {
        var card = new Border
        {
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 9, 12, 9),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });

        grid.Children.Add(new TextBlock
        {
            Text = row.Name,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        if (!row.PingOnly)
        {
            grid.Children.Add(MakeMetricCell(1, Loc.T("tools.test_site"), row.Http));
            grid.Children.Add(MakeMetricCell(2, "TLS 1.2", row.Tls12));
            grid.Children.Add(MakeMetricCell(3, "TLS 1.3", row.Tls13));
        }
        else
        {
            var pingOnly = MakeMetricCell(1, Loc.T("tools.test_ping"), row.Ping);
            Grid.SetColumnSpan(pingOnly, 3);
            grid.Children.Add(pingOnly);
        }

        card.Child = grid;
        return card;
    }

    private UIElement MakeMetricCell(int column, string label, string token)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = label + " ",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11
        });
        panel.Children.Add(new TextBlock
        {
            Text = DiagnosticStatusHelper.ToDisplayText(token),
            Foreground = DiagnosticStatusHelper.ToBrush(token),
            FontWeight = FontWeights.SemiBold,
            FontSize = 11.5
        });
        Grid.SetColumn(panel, column);
        return panel;
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
            VerticalAlignment = VerticalAlignment.Center,
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
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
        };

    private static TextBlock MakeMuted(string text) =>
        new()
        {
            Text = text,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };

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
            _ansi.Reset();
            _ansi.Clear(_output);
            _autoResponder = new TestScriptAutoResponder(_selectedKind, _terminal);
            _autoResponder.Reset();
            _tracker.BeginRun(_selectedKind);
            RefreshVisualState();

            await _terminal.StartPowerShellScriptAsync(script, _paths.Root);
            _startBtn.IsEnabled = false;
            _stopBtn.IsEnabled = true;
            _input.IsEnabled = true;
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
            SetIdleUi();
        }
    }

    private async Task StopTestAsync()
    {
        await _terminal.StopAsync();
        _ansi.Flush(_output);
        _tracker.StopRun();
        SetIdleUi();
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
            ConsoleLog.Instance.Write(Loc.F("strategies.log_run", fileName));
            UiHelpers.ShowInfo(Loc.F("tools.test_applied", StrategyDisplayHelper.ToDisplayName(fileName)));
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void OnTerminalOutput(string chunk)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                _tracker.Feed(chunk);
                _ansi.Append(_output, chunk);
                if (_autoResponder is not null)
                    await _autoResponder.FeedAsync(chunk);
            }
            catch (Exception ex)
            {
                UiHelpers.ShowError(ex.Message);
            }
        });
    }

    private void OnTerminalError(string message) =>
        Dispatcher.InvokeAsync(() => _ansi.Append(_output, $"{Loc.T("common.error_prefix")} {message}{Environment.NewLine}"));

    private void OnTerminalExited(int exitCode)
    {
        Dispatcher.Invoke(() =>
        {
            _ansi.Flush(_output);
            _tracker.EndRun(exitCode);
            SetIdleUi();
        });
    }

    private void SetIdleUi()
    {
        _startBtn.IsEnabled = true;
        _stopBtn.IsEnabled = false;
        _input.IsEnabled = false;
        RefreshVisualState();
    }

    private void OnTrackerChanged() => Dispatcher.Invoke(RefreshVisualState);

    private async void OnUnloadedAsync(object sender, RoutedEventArgs e)
    {
        _tracker.Changed -= OnTrackerChanged;
        _terminal.OutputReceived -= OnTerminalOutput;
        _terminal.ProcessExited -= OnTerminalExited;
        _terminal.ErrorOccurred -= OnTerminalError;
        await _terminal.DisposeAsync();
    }
}
