using System.IO;

namespace ZapretUI.Services;

/// <summary>
/// Встроенные стратегии Zapret UI (копируются в папку zapret при установке/запуске).
/// </summary>
public static class BundledStrategiesService
{
    public const string FeaturedStrategy = "Niko_ALT11.bat";

    private static readonly string[] Protected =
    [
        FeaturedStrategy
    ];

    public static string BundledDirectory =>
        Path.Combine(AppContext.BaseDirectory, "packaging", "strategies");

    public static bool IsProtected(string batFileName) =>
        Protected.Contains(batFileName, StringComparer.OrdinalIgnoreCase);

    public static void DeployTo(string zapretRoot)
    {
        if (!ZapretPaths.IsValidZapretRoot(zapretRoot))
            return;

        var sourceDir = BundledDirectory;
        if (!Directory.Exists(sourceDir))
            return;

        foreach (var sourceFile in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(sourceFile);
            if (string.IsNullOrEmpty(name))
                continue;

            var dest = Path.Combine(zapretRoot, name);
            if (File.Exists(dest))
                continue;

            try
            {
                File.Copy(sourceFile, dest);
            }
            catch
            {
                // ignore locked files
            }
        }
    }
}
