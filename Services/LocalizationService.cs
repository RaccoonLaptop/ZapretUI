using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ZapretUI.Services;

public static class LocalizationService
{
    private static Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
    private static string _language = "ru";

    public static string Language => _language;
    public static bool IsEnglish => _language == "en";

    public static void Initialize(string? language)
    {
        _language = NormalizeLanguage(language);
        var culture = new CultureInfo(_language == "en" ? "en-US" : "ru-RU");
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        LoadStrings(_language);
    }

    public static string T(string key) =>
        _strings.TryGetValue(key, out var value) ? value : key;

    public static string F(string key, params object[] args)
    {
        var template = T(key);
        try { return string.Format(template, args); }
        catch { return template; }
    }

    public static void RestartApplication()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
            {
                UseShellExecute = true
            });
        }
        System.Windows.Application.Current.Shutdown();
    }

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";

    private static void LoadStrings(string language)
    {
        var suffix = language == "en" ? "en" : "ru";
        var resourceName = $"ZapretUI.Resources.Strings.{suffix}.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
