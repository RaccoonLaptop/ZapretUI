using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZapretUI.Helpers;

public static class UiHelpers
{
    public static void ShowError(string message, Window? owner = null) =>
        MessageWindow.Show(message, Loc.T("app.title"), owner ?? GetActiveWindow());

    public static void ShowInfo(string message, Window? owner = null) =>
        MessageWindow.Show(message, Loc.T("app.title"), owner ?? GetActiveWindow());

    public static void ShowResult(Window? owner, string title, string text) =>
        ResultWindow.Show(owner, title, text);

    public static bool Confirm(string message, Window? owner = null) =>
        ConfirmWindow.Show(message, owner ?? GetActiveWindow());

    private static Window? GetActiveWindow() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current?.MainWindow;

    public static Button CreateNavButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Style = Application.Current.FindResource("NavButton") as Style,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 2, 0, 2)
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    public static TextBlock CreateSectionHeader(string text) => new()
    {
        Text = text,
        FontSize = 13,
        FontWeight = FontWeights.SemiBold,
        Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
        Margin = new Thickness(0, 16, 0, 8)
    };

    public static void OpenFolder(string path)
    {
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
        {
            ShowError(Loc.F("common.folder_missing", full));
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = full,
            UseShellExecute = true
        });
    }
}
