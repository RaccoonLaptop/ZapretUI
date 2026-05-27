using System.IO;

namespace ZapretUI.Services;

public sealed class ListFileService
{
    private readonly ZapretPaths _paths;

    public ListFileService(ZapretPaths paths) => _paths = paths;

    public string ReadList(string fileName) =>
        File.ReadAllText(Path.Combine(_paths.Lists, fileName));

    public void SaveList(string fileName, string content) =>
        File.WriteAllText(Path.Combine(_paths.Lists, fileName), content);

    public int CountLines(string fileName)
    {
        var path = Path.Combine(_paths.Lists, fileName);
        if (!File.Exists(path)) return 0;
        return File.ReadAllLines(path).Count(l => !string.IsNullOrWhiteSpace(l));
    }

    public bool ListExists(string fileName) =>
        File.Exists(Path.Combine(_paths.Lists, fileName));

    public void CreateList(string fileName, string? initialContent = null)
    {
        Directory.CreateDirectory(_paths.Lists);
        var path = Path.Combine(_paths.Lists, fileName);
        if (!File.Exists(path))
            File.WriteAllText(path, initialContent ?? Environment.NewLine);
    }

    public void EnsureUserLists()
    {
        Directory.CreateDirectory(_paths.Lists);
        EnsureFile("ipset-exclude-user.txt", "203.0.113.113/32");
        EnsureFile("list-general-user.txt", "domain.example.abc");
        EnsureFile("list-exclude-user.txt", "domain.example.abc");
    }

    private void EnsureFile(string name, string defaultLine)
    {
        var path = Path.Combine(_paths.Lists, name);
        if (!File.Exists(path))
            File.WriteAllText(path, defaultLine + Environment.NewLine);
    }
}
