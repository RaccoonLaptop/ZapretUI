using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Models;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class TestStrategiesPage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private Border _modeStandard = null!;
    private Border _modeDpi = null!;
    private Button _startBtn = null!;
    private PresetTestRunPanel _runPanel = null!;
    private PresetTestKind _selectedKind = PresetTestKind.Standard;

    public TestStrategiesPage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new StackPanel();

        root.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test"),
            FontSize = 28,
            FontWeight = FontWeights.Bold
        });
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_subtitle_visual"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 16)
        });

        var modeRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
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

        _startBtn = new Button
        {
            Content = Loc.T("tools.test_start"),
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 180,
            Padding = new Thickness(20, 12, 20, 12)
        };
        _startBtn.Click += async (_, _) => await BeginTestFlowAsync();
        root.Children.Add(_startBtn);

        _runPanel = new PresetTestRunPanel(_paths, _strategy, _settings)
        {
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_runPanel);

        Content = root;
        SelectMode(PresetTestKind.Standard);
    }

    private async Task BeginTestFlowAsync()
    {
        var strategies = StrategyDisplayHelper.LoadItems(_paths.Root, _paths.GetStrategyFiles());
        var owner = Window.GetWindow(this);
        if (!PresetTestSetupWindow.TryShow(owner, strategies, out var scope))
            return;

        _runPanel.Visibility = Visibility.Visible;
        await _runPanel.StartAsync(_selectedKind, scope);
    }

    private void SelectMode(PresetTestKind kind)
    {
        _selectedKind = kind;
        SetModeSelected(_modeStandard, kind == PresetTestKind.Standard);
        SetModeSelected(_modeDpi, kind == PresetTestKind.DpiFreeze);
    }

    private static Border CreateModeCard(string title, string hint, bool selected)
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
        stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 14 });
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

    private static void SetModeSelected(Border card, bool selected)
    {
        card.BorderBrush = (Brush)Application.Current.FindResource(selected ? "AccentBrush" : "BorderBrush");
        card.BorderThickness = new Thickness(selected ? 2 : 1);
        card.Background = (Brush)Application.Current.FindResource(selected ? "PanelOverlayBrush" : "InputBrush");
    }
}
