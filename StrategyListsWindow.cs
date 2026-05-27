using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI;

public sealed class StrategyListsWindow : Window
{
    private readonly ZapretPaths _paths;
    private readonly ListFileService _lists;
    private readonly string _strategyFile;
    private readonly Func<string> _getBatContent;

    private ListBox _list = null!;
    private TextBox _editor = null!;
    private TextBlock _info = null!;
    private Button _createBtn = null!;
    private Button _deleteBtn = null!;
    private string? _currentFile;

    public StrategyListsWindow(ZapretPaths paths, string strategyFile, Func<string> getBatContent)
    {
        _paths = paths;
        _strategyFile = strategyFile;
        _getBatContent = getBatContent;
        _lists = new ListFileService(paths);
        _lists.EnsureUserLists();

        Title = $"Списки — {strategyFile}";
        Width = 900;
        Height = 560;
        MinWidth = 700;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("BgBrush");

        BuildUi();
        RefreshList();
    }

    private void BuildUi()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new TextBlock
        {
            Text = $"Списки конфига: {_strategyFile}",
            FontSize = 20,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = "Показаны только .txt из папки lists/, которые указаны в выбранном .bat. " +
                   "После добавления нового списка в конфиг нажмите «Обновить».",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 6, 0, 0)
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 12, 0)
        };
        var leftStack = new DockPanel();
        _list = new ListBox { BorderThickness = new Thickness(0) };
        _list.SelectionChanged += (_, _) => LoadSelected();
        DockPanel.SetDock(_list, Dock.Top);
        leftStack.Children.Add(_list);

        var leftBtns = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        var refreshBtn = new Button
        {
            Content = "Обновить из конфига",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 0, 6)
        };
        refreshBtn.Click += (_, _) => RefreshList();
        leftBtns.Children.Add(refreshBtn);
        leftStack.Children.Add(leftBtns);
        left.Child = leftStack;
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
        var saveBtn = new Button
        {
            Content = "Сохранить",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        saveBtn.Click += (_, _) => SaveCurrent();
        _createBtn = new Button
        {
            Content = "Создать файл списка",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Visibility = Visibility.Collapsed
        };
        _createBtn.Click += (_, _) => CreateMissingList();
        _deleteBtn = new Button
        {
            Content = "Удалить файл",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Visibility = Visibility.Collapsed
        };
        _deleteBtn.Click += (_, _) => DeleteCurrentList();
        toolbar.Children.Add(saveBtn);
        toolbar.Children.Add(_createBtn);
        toolbar.Children.Add(_deleteBtn);
        rightStack.Children.Add(toolbar);

        _editor = new TextBox
        {
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            IsReadOnly = false
        };
        rightStack.Children.Add(_editor);
        right.Child = rightStack;
        Grid.SetColumn(right, 1);
        split.Children.Add(right);

        Grid.SetRow(split, 1);
        root.Children.Add(split);
        Content = root;
    }

    private void RefreshList()
    {
        var bat = _getBatContent();
        var resolved = StrategyListResolver.Resolve(bat, _paths);

        _list.Items.Clear();
        foreach (var item in resolved)
        {
            var label = item.ExistsOnDisk
                ? item.FileName
                : $"{item.FileName}  (в конфиге, файл ещё не создан)";
            _list.Items.Add(new ListEntry(item.FileName, item.ExistsOnDisk, label));
        }

        if (_list.Items.Count == 0)
        {
            _info.Text = "В конфиге не найдено ссылок на lists/*.txt";
            _editor.Text = "";
            _currentFile = null;
            _createBtn.Visibility = Visibility.Collapsed;
            _deleteBtn.Visibility = Visibility.Collapsed;
            return;
        }

        _list.SelectedIndex = 0;
    }

    private void LoadSelected()
    {
        if (_list.SelectedItem is not ListEntry entry) return;
        _currentFile = entry.FileName;

        if (!entry.ExistsOnDisk)
        {
            _editor.Text = "";
            _editor.IsReadOnly = true;
            _info.Text = $"{entry.FileName} — указан в конфиге, но файла нет в lists/";
            _createBtn.Visibility = Visibility.Visible;
            _deleteBtn.Visibility = Visibility.Collapsed;
            return;
        }

        _editor.IsReadOnly = false;
        _createBtn.Visibility = Visibility.Collapsed;
        _deleteBtn.Visibility = _lists.CanDeleteList(entry.FileName)
            ? Visibility.Visible
            : Visibility.Collapsed;
        try
        {
            _editor.Text = _lists.ReadList(entry.FileName);
            var lines = _lists.CountLines(entry.FileName);
            var kind = entry.FileName.EndsWith("-user.txt", StringComparison.OrdinalIgnoreCase)
                ? "пользовательский"
                : "основной";
            _info.Text = $"{entry.FileName} ({kind}) — {lines} записей";
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void SaveCurrent()
    {
        if (_currentFile is null) return;
        if (_list.SelectedItem is ListEntry entry && !entry.ExistsOnDisk)
        {
            UiHelpers.ShowError("Сначала создайте файл списка");
            return;
        }

        try
        {
            _lists.SaveList(_currentFile, _editor.Text);
            _info.Text = $"{_currentFile} — {_lists.CountLines(_currentFile)} записей (сохранено)";
            ConsoleLog.Instance.Write($"Список сохранён: {_currentFile}");
            RefreshList();
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void CreateMissingList()
    {
        if (_currentFile is null) return;
        try
        {
            _lists.CreateList(_currentFile, "# Добавьте домены или IP (по одному на строку)" + Environment.NewLine);
            ConsoleLog.Instance.Write($"Создан список: {_currentFile}");
            RefreshList();
            SelectFile(_currentFile);
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void DeleteCurrentList()
    {
        if (_currentFile is null) return;
        if (!_lists.CanDeleteList(_currentFile))
        {
            UiHelpers.ShowError(
                "Нельзя удалить встроенные списки (list-general.txt, list-google.txt и т.п.). " +
                "Можно удалять пользовательские (*-user.txt) и созданные вручную файлы.");
            return;
        }

        if (!UiHelpers.Confirm($"Удалить файл списка {_currentFile} из папки lists/?"))
            return;

        try
        {
            _lists.DeleteList(_currentFile);
            ConsoleLog.Instance.Write($"Удалён список: {_currentFile}");
            _currentFile = null;
            RefreshList();
            UiHelpers.ShowInfo("Файл списка удалён");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void SelectFile(string fileName)
    {
        for (var i = 0; i < _list.Items.Count; i++)
        {
            if (_list.Items[i] is ListEntry e &&
                e.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                _list.SelectedIndex = i;
                return;
            }
        }
    }

    private sealed record ListEntry(string FileName, bool ExistsOnDisk, string Label)
    {
        public override string ToString() => Label;
    }
}
