using System.Windows;
using System.Windows.Controls;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class StrategiesPage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private ListBox _list = null!;
    private TextBox _editor = null!;
    private TextBlock _fileName = null!;
    private string? _currentFile;

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

        // Header
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        header.Children.Add(new TextBlock { Text = "Стратегии", FontSize = 28, FontWeight = FontWeights.Bold });
        header.Children.Add(new TextBlock
        {
            Text = "Конфиги запуска Zapret (.bat). Редактируйте или создайте свои.",
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
        });
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // Main split
        var split = new Grid();
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left panel - list
        var leftPanel = new Border
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

        var leftBtns = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        var runBtn = new Button { Content = "▶ Запустить", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 0, 8) };
        runBtn.Click += async (_, _) => await RunSelectedAsync();
        var createBtn = new Button { Content = "+ Создать копию", Style = (Style)Application.Current.FindResource("SecondaryButton"), Margin = new Thickness(0, 0, 0, 8) };
        createBtn.Click += (_, _) => CreateCopy();
        var newBtn = new Button { Content = "+ Новый конфиг", Style = (Style)Application.Current.FindResource("SecondaryButton") };
        newBtn.Click += (_, _) => CreateNew();
        leftBtns.Children.Add(runBtn);
        leftBtns.Children.Add(createBtn);
        leftBtns.Children.Add(newBtn);
        leftStack.Children.Add(leftBtns);
        leftPanel.Child = leftStack;
        Grid.SetColumn(leftPanel, 0);
        split.Children.Add(leftPanel);

        // Right panel - editor
        var rightPanel = new Border
        {
            Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16)
        };
        var rightStack = new DockPanel();
        _fileName = new TextBlock { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(_fileName, Dock.Top);
        rightStack.Children.Add(_fileName);

        var editorToolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(editorToolbar, Dock.Top);
        var saveBtn = new Button { Content = "💾 Сохранить", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 8, 0) };
        saveBtn.Click += (_, _) => SaveCurrent();
        var revertBtn = new Button { Content = "↩ Отменить", Style = (Style)Application.Current.FindResource("SecondaryButton") };
        revertBtn.Click += (_, _) => LoadSelected();
        editorToolbar.Children.Add(saveBtn);
        editorToolbar.Children.Add(revertBtn);
        rightStack.Children.Add(editorToolbar);

        _editor = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 13
        };
        rightStack.Children.Add(_editor);
        rightPanel.Child = rightStack;
        Grid.SetColumn(rightPanel, 1);
        split.Children.Add(rightPanel);

        Grid.SetRow(split, 1);
        grid.Children.Add(split);
        Content = grid;
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var f in _paths.GetStrategyFiles())
            _list.Items.Add(f);
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    private void LoadSelected()
    {
        if (_list.SelectedItem is not string file) return;
        _currentFile = file;
        _fileName.Text = file;
        try
        {
            _editor.Text = _strategy.ReadStrategyContent(file);
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
        if (_list.SelectedItem is not string file) return;
        _settings.LastStrategy = file;
        _settings.Save();
        try
        {
            await _strategy.StartStrategyAsync(file);
            ConsoleLog.Instance.Write($"Запущена стратегия: {file}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void CreateCopy()
    {
        if (_list.SelectedItem is not string baseFile) return;
        var name = Prompt("Имя нового конфига", baseFile.Replace(".bat", " (custom).bat"));
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            var created = _strategy.CreateCustomStrategy(baseFile, name);
            RefreshList();
            _list.SelectedItem = created;
            ConsoleLog.Instance.Write($"Создан конфиг: {created}");
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private void CreateNew()
    {
        var name = Prompt("Имя нового конфига", "my-strategy.bat");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!name.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            name += ".bat";

        var template = _paths.GetStrategyFiles().FirstOrDefault() ?? "general.bat";
        try
        {
            var created = _strategy.CreateCustomStrategy(template, name);
            RefreshList();
            _list.SelectedItem = created;
        }
        catch (Exception ex)
        {
            UiHelpers.ShowError(ex.Message);
        }
    }

    private static string? Prompt(string title, string defaultValue)
    {
        var dlg = new Window
        {
            Title = title,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = (Brush)Application.Current.FindResource("BgBrush"),
            ResizeMode = ResizeMode.NoResize
        };
        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 8) });
        var input = new TextBox { Text = defaultValue };
        stack.Children.Add(input);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        string? result = null;
        var ok = new Button { Content = "OK", Style = (Style)Application.Current.FindResource("PrimaryButton"), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        ok.Click += (_, _) => { result = input.Text; dlg.Close(); };
        var cancel = new Button { Content = "Отмена", Style = (Style)Application.Current.FindResource("SecondaryButton"), IsCancel = true };
        cancel.Click += (_, _) => dlg.Close();
        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        stack.Children.Add(btns);
        dlg.Content = stack;
        dlg.ShowDialog();
        return result;
    }
}
