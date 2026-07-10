using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace ZapretUI.Helpers;

public static class MouseWheelScrollHelper
{
    public static void Attach(TextBox textBox)
    {
        textBox.PreviewMouseWheel += (_, e) =>
        {
            ScrollTextBox(textBox, e);
            e.Handled = true;
        };
    }

    public static void Attach(TextEditor editor)
    {
        editor.PreviewMouseWheel += (_, e) =>
        {
            var steps = Math.Max(1, Math.Abs(e.Delta) / 120) * 3;
            var line = editor.TextArea.Caret.Line;
            var target = e.Delta > 0
                ? Math.Max(0, line - steps)
                : Math.Min(Math.Max(0, editor.LineCount - 1), line + steps);
            editor.ScrollToLine(target);
            e.Handled = true;
        };
    }

    public static void Attach(ListBox listBox)
    {
        listBox.PreviewMouseWheel += (_, e) =>
        {
            var scrollViewer = FindScrollViewer(listBox);
            if (scrollViewer is null) return;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        };
    }

    public static void Attach(ScrollViewer scrollViewer)
    {
        scrollViewer.PreviewMouseWheel += (_, e) =>
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        };
    }

    private static void ScrollTextBox(TextBox textBox, MouseWheelEventArgs e)
    {
        var steps = Math.Max(1, Math.Abs(e.Delta) / 120);
        for (var i = 0; i < steps; i++)
        {
            if (e.Delta > 0)
                textBox.LineUp();
            else
                textBox.LineDown();
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv)
            return sv;

        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            var found = FindScrollViewer(child);
            if (found is not null)
                return found;
        }

        return null;
    }
}
