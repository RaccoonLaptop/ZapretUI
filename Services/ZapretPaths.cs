using System.IO;
using System.Text.RegularExpressions;

namespace ZapretUI.Services;

public sealed class ZapretPaths
{
    public string Root { get; }
    public string Bin => Path.Combine(Root, "bin");
    public string Lists => Path.Combine(Root, "lists");
    public string Utils => Path.Combine(Root, "utils");
    public string ServiceBat => Path.Combine(Root, "service.bat");

    public bool IsValid => IsValidZapretRoot(Root);

    public ZapretPaths(string? rootOverride = null)
    {
        Root = rootOverride ?? DetectRoot();
    }

    public static bool IsValidZapretRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            var full = Path.GetFullPath(path);
            return File.Exists(Path.Combine(full, "service.bat")) &&
                   Directory.Exists(Path.Combine(full, "bin"));
        }
        catch
        {
            return false;
        }
    }

    public static string DetectRoot(string? savedPath = null)
    {
        if (IsValidZapretRoot(savedPath))
            return Path.GetFullPath(savedPath!);

        var dir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        for (var i = 0; i < 8; i++)
        {
            if (IsValidZapretRoot(dir))
                return Path.GetFullPath(dir);

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent is null) break;
            dir = parent;
        }

        // Рядом с папкой программы (ZapretUI-Program лежит внутри zapret или рядом)
        var sibling = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
        if (IsValidZapretRoot(sibling))
            return sibling;

        return savedPath ?? sibling;
    }

    public string GetLocalVersion()
    {
        if (!File.Exists(ServiceBat)) return "unknown";
        var text = File.ReadAllText(ServiceBat);
        var match = Regex.Match(text, @"set\s+""LOCAL_VERSION=([^""]+)""");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    public IEnumerable<string> GetStrategyFiles()
    {
        if (!IsValid) return [];
        return Directory.GetFiles(Root, "*.bat")
            .Select(Path.GetFileName)
            .Where(f => f is not null && !f.StartsWith("service", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Cast<string>();
    }

    public IEnumerable<string> GetListFiles()
    {
        if (!IsValid || !Directory.Exists(Lists)) return [];
        return Directory.GetFiles(Lists, "*.txt")
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Cast<string>();
    }
}
