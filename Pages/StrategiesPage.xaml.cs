using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class StrategiesPage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private ListBox _list = null!;
    private TextEditor _editor = null!;
    private TextBlock _fileName = null!;
    private TextBlock _runStatus = null!;
    private Button _runBtn = null!;
    private string? _currentFile;
    private bool _isStarting;

    public StrategiesPage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        BuildUi();
        RefreshList();
    }

    private void BuildUi()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        header.Children.Add(new TextBlock { Text = "Стратегии", FontSize = 28, FontWeight = FontWeights.Bold });
        header.Children.Add(new TextBlock
        {
            Text = "Конфиги запуска Zapret (.bat). Выберите файл слева, отредактируйте и сохраните.",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        header.Children.Add(new TextBlock
        {
            Text = "Конфиг Niko_ALT11 — авторская стратегия Niko. «Списки» — .txt из выбранного .bat.",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 0)
        });
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftPanel = new Border
        {
            Background = (Brush)Application.Current.FindResource("PanelOverlayBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 12, 0)
        };
        var leftStack = new DockPanel();
        _list = new ListBox { BorderThickness = new Thickness(0) };
        _list.SelectionChanged += (_, _) => LoadSelected();
        DockPanel.SetDock(_list, Dock.Top);
        leftStack.Children.Add(_list);

        var leftBtns = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        _runBtn = new Button { Content = "Запустить", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 0, 8) };
        _runBtn.Click += async (_, _) => await RunSelectedAsync();
        var newBtn = new Button { Content = "Новый конфиг", Style = (Style)Application.Current.FindResource("SecondaryButton") };
        newBtn.Click += (_, _) => CreateNew();
        leftBtns.Children.Add(_runBtn);
        leftBtns.Children.Add(newBtn);
        _runStatus = new TextBlock
        {
            Text = "",
            Visibility = Visibility.Collapsed,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        leftBtns.Children.Add(_runStatus);
        leftStack.Children.Add(leftBtns);
        leftPanel.Child = leftStack;
        Grid.SetColumn(leftPanel, 0);
        split.Children.Add(leftPanel);

        var rightPanel = new Border
        {
            Background = (Brush)Application.Current.FindResource("PanelOverlayBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16)
        };
        var rightStack = new DockPanel();
        _fileName = new TextBlock { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(_fileName, Dock.Top);
        rightStack.Children.Add(_fileName);

        var editorToolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(editorToolbar, Dock.Top);

        var saveBtn = MakeToolbarBtn("Сохранить", "PrimaryButton", (_, _) => SaveCurrent());
        var revertBtn = MakeToolbarBtn("Отменить", "SecondaryButton", (_, _) => LoadSelected());
        var copyBtn = MakeToolbarBtn("Создать копию", "SecondaryButton", (_, _) => CreateCopy());
        var renameBtn = MakeToolbarBtn("Переименовать", "SecondaryButton", (_, _) => RenameCurrent());
        var deleteBtn = MakeToolbarBtn("Удалить", "SecondaryButton", (_, _) => DeleteCurrent());
        var listsBtn = MakeToolbarBtn("Списки", "SecondaryButton", (_, _) => OpenListsWindow());
        var findBtn = MakeToolbarBtn("Найти / заменить", "SecondaryButton", (_, _) => OpenFindReplace());

        editorToolbar.Children.Add(saveBtn);
        editorToolbar.Children.Add(revertBtn);
        editorToolbar.Children.Add(copyBtn);
        editorToolbar.Children.Add(renameBtn);
        editorToolbar.Children.Add(deleteBtn);
        editorToolbar.Children.Add(listsBtn);
        editorToolbar.Children.Add(findBtn);
        rightStack.Children.Add(editorToolbar);

        _editor = new TextEditor
        {
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            ShowLineNumbers = true,
            WordWrap = false,
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("InputBrush"),
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextBrush"),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        BatchSyntaxHighlighting.Apply(_editor);
        _editor.InputBindings.Add(new KeyBinding(
            new RelayCommand(OpenFindReplace),
            Key.F,
            ModifierKeys.Control));
        rightStack.Children.Add(_editor);
        rightPanel.Child = rightStack;
        Grid.SetColumn(rightPanel, 1);
        split.Children.Add(rightPanel);

        Grid.SetRow(split, 1);
        grid.Children.Add(split);
        Content = grid;
    }

    private static Button MakeToolbarBtn(string text, string styleKey, RoutedEventHandler click)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource(styleKey),
            Margin = new Thickness(0, 0, 8, 8)
        };
        btn.Click += click;
        return btn;
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var f in _paths.GetStrategyFiles())
            _list.Items.Add(f);
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    private void OpenListsWindow()
    {
        var file = _currentFile ?? _list.SelectedItem as string;
        if (string.IsNullOrEmpty(file))
        {
            UiHelpers.ShowError("Сначала выберите конфиг (.bat) слева");
            return;
        }

        var owner = Window.GetWindow(this);
        var win = new StrategyListsWindow(_paths, file, () => _editor.Text)
        {
            Owner = owner
        };
        win.Show();
    }

    private void LoadSelected()
    {
        if (_list.SelectedItem is not string file) return;
        _currentFile = file;
        _fileName.Text = file;
        try
        {
            _editor.Text = _strategy.ReadStrategyContent(file);
            BatchSyntaxHighlighting.Apply(_editor);
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
            _strategy.SaveStrategyContent(_currentFile, _editor.Text);
            ConsoleLog.Instance.Write($"Сохранено: {_currentFile}");
            UiHelpers.ShowInfo("Файл сохранён");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private async Task RunSelectedAsync()
    {
        if (_isStarting) return;
        if (_list.SelectedItem is not string file) return;
        _settings.LastStrategy = file;
        _settings.Save();
        try
        {
            _isStarting = true;
            _runBtn.IsEnabled = false;
            _runBtn.Content = "Запускается...";
            _runStatus.Text = "Подготовка запуска...";
            _runStatus.Visibility = Visibility.Visible;

            _runStatus.Text = "Запускаем winws, подождите...";
            await _strategy.StartStrategyAsync(file);
            ConsoleLog.Instance.Write($"Запущена стратегия: {file}");
            _runStatus.Text = "Запуск завершен";
        }
        catch (Exception ex)
        {
            _runStatus.Text = "Ошибка запуска";
            UiHelpers.ShowError(ex.Message);
        }
        finally
        {
            _isStarting = false;
            _runBtn.IsEnabled = true;
            _runBtn.Content = "Запустить";
            if (_strategy.IsRunning())
            {
                _ = Task.Delay(1200).ContinueWith(_ =>
                    Dispatcher.Invoke(() => _runStatus.Visibility = Visibility.Collapsed));
            }
        }
    }

    private void CreateCopy()
    {
        if (_list.SelectedItem is not string baseFile) return;
        var name = Prompt(
            "Создать копию",
            "Введите имя для копии (будет создан новый .bat на основе выбранного файла):",
            baseFile.Replace(".bat", "-copy.bat"));
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var created = _strategy.CreateCustomStrategy(baseFile, name);
            RefreshList();
            _list.SelectedItem = created;
            ConsoleLog.Instance.Write($"Создан конфиг: {created}");
            UiHelpers.ShowInfo($"Создана копия: {created}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void RenameCurrent()
    {
        if (_currentFile is null) return;

        if (_currentFile.StartsWith("general", StringComparison.OrdinalIgnoreCase) &&
            !_currentFile.Contains("custom", StringComparison.OrdinalIgnoreCase) &&
            !_currentFile.Contains("copy", StringComparison.OrdinalIgnoreCase))
        {
            if (!UiHelpers.Confirm($"Переименовать базовый конфиг {_currentFile}?\nЛучше создать копию, чтобы не потерять оригинал."))
                return;
        }

        var name = Prompt(
            "Переименовать",
            "Новое имя файла:",
            _currentFile);
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var renamed = _strategy.RenameStrategy(_currentFile, name);
            if (_settings.LastStrategy == _currentFile)
            {
                _settings.LastStrategy = renamed;
                _settings.Save();
            }
            RefreshList();
            _list.SelectedItem = renamed;
            ConsoleLog.Instance.Write($"Переименовано: {_currentFile} → {renamed}");
            UiHelpers.ShowInfo($"Файл переименован в {renamed}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void OpenFindReplace() =>
        FindReplaceDialog.ShowFor(_editor, Window.GetWindow(this));

    private void DeleteCurrent()
    {
        if (_currentFile is null) return;

        if (!_strategy.CanDeleteStrategy(_currentFile))
        {
            var msg = BundledStrategiesService.IsProtected(_currentFile)
                ? $"Нельзя удалить встроенный конфиг {_currentFile}."
                : "Нельзя удалить служебный файл service.bat";
            UiHelpers.ShowError(msg);
            return;
        }

        if (_currentFile.StartsWith("general", StringComparison.OrdinalIgnoreCase) &&
            !_currentFile.Contains("custom", StringComparison.OrdinalIgnoreCase) &&
            !_currentFile.Contains("copy", StringComparison.OrdinalIgnoreCase))
        {
            if (!UiHelpers.Confirm(
                    $"Удалить базовый конфиг {_currentFile}?\nРекомендуется удалять только копии и свои файлы."))
                return;
        }
        else if (!UiHelpers.Confirm($"Удалить конфиг {_currentFile}? Файл будет удалён с диска."))
        {
            return;
        }

        try
        {
            var deleted = _currentFile;
            _strategy.DeleteStrategy(deleted);
            if (_settings.LastStrategy == deleted)
            {
                _settings.LastStrategy = null;
                _settings.Save();
            }
            _currentFile = null;
            RefreshList();
            ConsoleLog.Instance.Write($"Удалён конфиг: {deleted}");
            UiHelpers.ShowInfo($"Удалён: {deleted}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void CreateNew()
    {
        var name = Prompt(
            "Новый конфиг",
            "Введите имя нового .bat (на основе general.bat):",
            "my-strategy.bat");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!name.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            name += ".bat";

        var template = _paths.GetStrategyFiles().FirstOrDefault(f => f.Equals("general.bat", StringComparison.OrdinalIgnoreCase))
                       ?? _paths.GetStrategyFiles().FirstOrDefault()
                       ?? "general.bat";
        try
        {
            var created = _strategy.CreateCustomStrategy(template, name);
            RefreshList();
            _list.SelectedItem = created;
            UiHelpers.ShowInfo($"Создан файл: {created}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
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
        var cancel = new Button { Content = "Отмена", Style = (Style)Application.Current.FindResource("SecondaryButton"), IsCancel = true };
        cancel.Click += (_, _) => dlg.Close();
        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        stack.Children.Add(btns);
        dlg.Content = stack;
        dlg.ShowDialog();
        return result;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _action;
        public RelayCommand(Action action) => _action = action;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
    }
}
