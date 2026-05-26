using System.Windows.Forms;

namespace ZapretUI.Helpers;

public static class FolderPicker
{
    public static string? PickFolder(string description, string? initialPath = null)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            dialog.SelectedPath = initialPath;

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
}
