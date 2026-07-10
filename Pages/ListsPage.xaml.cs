using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class ListsPage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly ListFileService _lists;

    private ListBox _list = null!;
    private TextBox _editor = null!;
    private TextBox _newDomain = null!;
    private TextBlock _title = null!;
    private Button _deleteBtn = null!;

    public ListsPage(ZapretPaths paths)
    {
        _paths = paths;
        _lists = new ListFileService(paths);
        _lists.EnsureUserLists();
        BuildUi();
        RefreshList();
    }

    private void BuildUi()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        header.Children.Add(new TextBlock { Text = Loc.T("lists.page_title"), FontSize = 28, FontWeight = FontWeights.Bold });
        header.Children.Add(new TextBlock
        {
            Text = Loc.T("lists.page_subtitle"),
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });
        var headerBtns = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        headerBtns.Children.Add(MakeBtn(Loc.T("strategies.open_lists_folder"), "SecondaryButton", (_, _) =>
            UiHelpers.OpenFolder(_paths.Lists)));
        header.Children.Add(headerBtns);
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = MakeCard();
        left.Margin = new Thickness(0, 0, 12, 0);
        var leftStack = new DockPanel();
        _list = new ListBox { BorderThickness = new Thickness(0) };
        _list.SelectionChanged += (_, _) => LoadSelected();
        DockPanel.SetDock(_list, Dock.Top);
        leftStack.Children.Add(_list);
        var newBtn = MakeBtn(Loc.T("lists.new_list"), "SecondaryButton", (_, _) => CreateNewList());
        newBtn.Margin = new Thickness(0, 10, 0, 0);
        leftStack.Children.Add(newBtn);
        left.Child = leftStack;
        Grid.SetColumn(left, 0);
        split.Children.Add(left);

        var right = MakeCard();
        var rightStack = new DockPanel();
        _title = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(_title, Dock.Top);
        rightStack.Children.Add(_title);

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(addRow, Dock.Top);
        _newDomain = new TextBox { Width = 280, Padding = new Thickness(8, 6, 8, 6) };
        var addBtn = MakeBtn(Loc.T("lists.add_domain"), "SecondaryButton", (_, _) => AddDomain());
        addBtn.Margin = new Thickness(8, 0, 0, 0);
        addRow.Children.Add(_newDomain);
        addRow.Children.Add(addBtn);
        rightStack.Children.Add(addRow);

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(toolbar, Dock.Top);
        toolbar.Children.Add(MakeBtn(Loc.T("lists.save"), "PrimaryButton", (_, _) => SaveCurrent()));
        _deleteBtn = MakeBtn(Loc.T("lists.delete"), "SecondaryButton", (_, _) => DeleteCurrent());
        _deleteBtn.Margin = new Thickness(8, 0, 0, 0);
        toolbar.Children.Add(_deleteBtn);
        rightStack.Children.Add(toolbar);

        _editor = new TextBox
        {
            AcceptsReturn = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = (Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8)
        };
        rightStack.Children.Add(_editor);
        right.Child = rightStack;
        Grid.SetColumn(right, 1);
        split.Children.Add(right);

        Grid.SetRow(split, 1);
        grid.Children.Add(split);
        Content = grid;
    }

    private void RefreshList()
    {
        var selected = _list.SelectedItem as string;
        _list.Items.Clear();
        foreach (var file in _lists.GetAllListFiles())
            _list.Items.Add(file);

        if (_list.Items.Count == 0)
        {
            _title.Text = Loc.T("lists.select_or_create");
            _editor.Text = "";
            _deleteBtn.IsEnabled = false;
            return;
        }

        var idx = 0;
        if (!string.IsNullOrEmpty(selected))
        {
            for (var i = 0; i < _list.Items.Count; i++)
            {
                if (string.Equals(_list.Items[i] as string, selected, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
        }
        _list.SelectedIndex = idx;
    }

    private void LoadSelected()
    {
        if (_list.SelectedItem is not string file)
        {
            _title.Text = Loc.T("lists.select_or_create");
            _editor.Text = "";
            _deleteBtn.IsEnabled = false;
            return;
        }

        _title.Text = file;
        _deleteBtn.IsEnabled = _lists.CanDeleteList(file);
        try
        {
            _editor.Text = _lists.ListExists(file) ? _lists.ReadList(file) : "";
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void SaveCurrent()
    {
        if (_list.SelectedItem is not string file) return;
        try
        {
            _lists.SaveList(file, _editor.Text);
            UiHelpers.ShowInfo(Loc.F("lists.info_saved", file, _lists.CountLines(file)));
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void CreateNewList()
    {
        var name = Prompt(Loc.T("lists.new_list"), Loc.T("lists.new_list_prompt"), "my-list.txt");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            name += ".txt";

        try
        {
            _lists.CreateList(name, Loc.T("lists.new_file_comment") + Environment.NewLine);
            RefreshList();
            _list.SelectedItem = name;
            LoadSelected();
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void AddDomain()
    {
        if (_list.SelectedItem is not string file) return;
        var domain = _newDomain.Text.Trim();
        if (string.IsNullOrWhiteSpace(domain)) return;

        var lines = _editor.Text.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();

        if (lines.Any(l => l.Equals(domain, StringComparison.OrdinalIgnoreCase)))
        {
            _newDomain.Clear();
            return;
        }

        lines.Add(domain);
        _editor.Text = string.Join(Environment.NewLine, lines) + Environment.NewLine;
        _newDomain.Clear();
        SaveCurrent();
    }

    private void DeleteCurrent()
    {
        if (_list.SelectedItem is not string file) return;
        if (!UiHelpers.Confirm(Loc.F("lists.delete_confirm", file))) return;

        try
        {
            _lists.DeleteList(file);
            RefreshList();
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private static Border MakeCard() =>
        new()
        {
            Background = (Brush)Application.Current.FindResource("PanelOverlayBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16)
        };

    private static Button MakeBtn(string text, string styleKey, RoutedEventHandler click)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource(styleKey),
            Margin = new Thickness(0, 0, 8, 0)
        };
        btn.Click += click;
        return btn;
    }

    private static string? Prompt(string title, string description, string defaultValue)
    {
        var dlg = new Window
        {
            Title = title,
            Width = 480,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = (Brush)Application.Current.FindResource("BgBrush"),
            ResizeMode = ResizeMode.NoResize
        };
        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 10)
        });
        var input = new TextBox { Text = defaultValue };
        stack.Children.Add(input);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        string? result = null;
        var ok = new Button { Content = "OK", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        ok.Click += (_, _) => { result = input.Text.Trim(); dlg.Close(); };
        var cancel = new Button { Content = Loc.T("common.cancel"), Style = (Style)Application.Current.FindResource("SecondaryButton"), IsCancel = true };
        cancel.Click += (_, _) => dlg.Close();
        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        stack.Children.Add(btns);
        dlg.Content = stack;
        dlg.ShowDialog();
        return result;
    }
}
