using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Models;

namespace ZapretUI;

public sealed class PresetTestSetupWindow : Window
{
    private readonly RadioButton _allRadio;
    private readonly RadioButton _oneRadio;
    private readonly ComboBox _strategyCombo;

    public bool Confirmed { get; private set; }
    public bool TestAll => _allRadio.IsChecked == true;
    public string? SelectedStrategyFile =>
        _strategyCombo.SelectedItem is StrategyItem item ? item.FileName : null;

    private PresetTestSetupWindow(IReadOnlyList<StrategyItem> strategies)
    {
        Title = Loc.T("tools.test_setup_title");
        Width = 520;
        Height = 360;
        MinWidth = 420;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("BgBrush");
        ResizeMode = ResizeMode.NoResize;

        var root = new StackPanel { Margin = new Thickness(22) };
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_setup_title"),
            FontSize = 20,
            FontWeight = FontWeights.Bold
        });
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_setup_subtitle"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 16)
        });

        _allRadio = new RadioButton
        {
            Content = Loc.T("tools.test_scope_all"),
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 8)
        };
        _oneRadio = new RadioButton
        {
            Content = Loc.T("tools.test_scope_one"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        root.Children.Add(_allRadio);
        root.Children.Add(_oneRadio);

        _strategyCombo = new ComboBox
        {
            Margin = new Thickness(20, 0, 0, 12),
            DisplayMemberPath = nameof(StrategyItem.DisplayName),
            SelectedValuePath = nameof(StrategyItem.FileName),
            IsEnabled = false
        };
        foreach (var item in strategies)
            _strategyCombo.Items.Add(item);
        if (_strategyCombo.Items.Count > 0)
            _strategyCombo.SelectedIndex = 0;
        root.Children.Add(_strategyCombo);

        _oneRadio.Checked += (_, _) => _strategyCombo.IsEnabled = true;
        _allRadio.Checked += (_, _) => _strategyCombo.IsEnabled = false;

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var ok = new Button
        {
            Content = Loc.T("tools.test_setup_start"),
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true,
            MinWidth = 120
        };
        ok.Click += (_, _) =>
        {
            if (_oneRadio.IsChecked == true && _strategyCombo.SelectedItem is null)
            {
                UiHelpers.ShowError(Loc.T("tools.test_scope_pick"), this);
                return;
            }
            Confirmed = true;
            Close();
        };
        var cancel = new Button
        {
            Content = Loc.T("common.cancel"),
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            IsCancel = true
        };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);
        Content = root;
    }

    public static bool TryShow(Window? owner, IReadOnlyList<StrategyItem> strategies, out PresetTestScope scope)
    {
        scope = new PresetTestScope();
        if (strategies.Count == 0)
        {
            UiHelpers.ShowError(Loc.T("tools.test_no_strategies"), owner);
            return false;
        }

        var dlg = new PresetTestSetupWindow(strategies) { Owner = owner };
        dlg.ShowDialog();
        if (!dlg.Confirmed) return false;

        scope = new PresetTestScope
        {
            TestAll = dlg.TestAll,
            SingleStrategyFile = dlg.TestAll ? null : dlg.SelectedStrategyFile
        };
        return true;
    }
}
