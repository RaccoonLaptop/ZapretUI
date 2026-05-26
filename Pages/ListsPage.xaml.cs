using System.Windows;
using System.Windows.Controls;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class ListsPage : UserControl
{
    private readonly ListFileService _lists;
    private readonly ZapretPaths _paths;
    private ListBox _list = null!;
    private TextBox _editor = null!;
    private TextBlock _info = null!;
    private string? _currentFile;

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
        header.Children.Add(new TextBlock { Text = "Списки", FontSize = 28, FontWeight = FontWeights.Bold });
        header.Children.Add(new TextBlock
        {
            Text = "Домены и IP для фильтрации. Файлы *-user.txt — ваши пользовательские записи.",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 12, 0)
        };
        _list = new ListBox { BorderThickness = new Thickness(0) };
        _list.SelectionChanged += (_, _) => LoadSelected();
        left.Child = _list;
        Grid.SetColumn(left, 0);
        split.Children.Add(left);

        var right = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16)
        };
        var rightStack = new DockPanel();
        _info = new TextBlock { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(_info, Dock.Top);
        rightStack.Children.Add(_info);

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(toolbar, Dock.Top);
        var saveBtn = new Button { Content = "💾 Сохранить", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 8, 0) };
        saveBtn.Click += (_, _) => SaveCurrent();
        toolbar.Children.Add(saveBtn);
        rightStack.Children.Add(toolbar);

        _editor = new TextBox
        {
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 13
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
        _list.Items.Clear();
        foreach (var f in _paths.GetListFiles())
            _list.Items.Add(f);
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    private void LoadSelected()
    {
        if (_list.SelectedItem is not string file) return;
        _currentFile = file;
        try
        {
            _editor.Text = _lists.ReadList(file);
            var lines = _lists.CountLines(file);
            _info.Text = $"{file} — {lines} записей";
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void SaveCurrent()
    {
        if (_currentFile is null) return;
        try
        {
            _lists.SaveList(_currentFile, _editor.Text);
            _info.Text = $"{_currentFile} — {_lists.CountLines(_currentFile)} записей (сохранено)";
            ConsoleLog.Instance.Write($"Список сохранён: {_currentFile}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }
}
