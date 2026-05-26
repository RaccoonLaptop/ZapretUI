using System.IO;
using System.Text.Json;

namespace ZapretUI.Services;

public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ZapretUI", "settings.json");

    public string? ZapretRoot { get; set; }
    public string? LastStrategy { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool AutoUpdateApp { get; set; } = true;
    public string? UpdateManifestUrl { get; set; }
    public string? LastInstalledVersion { get; set; }
    public bool SecuritySetupCompleted { get; set; }
    public bool SecuritySetupSkipped { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* ignore */ }
        return new AppSettings();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
