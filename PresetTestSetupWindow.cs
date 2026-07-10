using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Models;

namespace ZapretUI;

public sealed class PresetTestSetupWindow : Window
{
    private readonly CheckBox _allCheck;
    private readonly List<(CheckBox Box, StrategyItem Item)> _strategyChecks = [];

    public bool Confirmed { get; private set; }

    private PresetTestSetupWindow(IReadOnlyList<StrategyItem> strategies)
    {
        Title = Loc.T("tools.test_setup_title");
        Width = 520;
        Height = 480;
        MinWidth = 420;
        MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("BgBrush");
        ResizeMode = ResizeMode.CanResizeWithGrip;

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
            Margin = new Thickness(0, 8, 0, 12)
        });

        _allCheck = new CheckBox
        {
            Content = Loc.T("tools.test_scope_all"),
            IsChecked = true,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _allCheck.Checked += (_, _) => SetIndividualEnabled(false);
        _allCheck.Unchecked += (_, _) => SetIndividualEnabled(true);
        root.Children.Add(_allCheck);

        var listBorder = new Border
        {
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            MaxHeight = 260
        };
        var listScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var listStack = new StackPanel();
        foreach (var item in strategies)
        {
            var box = new CheckBox
            {
                Content = item.DisplayName,
                Tag = item,
                IsEnabled = false,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _strategyChecks.Add((box, item));
            listStack.Children.Add(box);
        }
        listScroll.Content = listStack;
        listBorder.Child = listScroll;
        root.Children.Add(listBorder);

        root.Children.Add(new TextBlock
        {
            Text = Loc.T("tools.test_scope_pick_hint"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        });

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
            if (_allCheck.IsChecked != true && !HasAnySelected())
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

    private void SetIndividualEnabled(bool enabled)
    {
        foreach (var (box, _) in _strategyChecks)
            box.IsEnabled = enabled;
    }

    private bool HasAnySelected() =>
        _strategyChecks.Any(entry => entry.Box.IsChecked == true);

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

        var testAll = dlg._allCheck.IsChecked == true;
        var selected = testAll
            ? (IReadOnlyList<string>)[]
            : dlg._strategyChecks
                .Where(entry => entry.Box.IsChecked == true)
                .Select(entry => entry.Item.FileName)
                .ToList();

        scope = new PresetTestScope
        {
            TestAll = testAll,
            SelectedStrategyFiles = selected
        };
        return true;
    }
}
