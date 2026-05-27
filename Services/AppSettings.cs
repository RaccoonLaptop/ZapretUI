using System.IO;
using System.Text.Json;
using ZapretUI.Controls.Backgrounds;

namespace ZapretUI.Services;

public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ZapretUI", "settings.json");

    public string? ZapretRoot { get; set; }
    public string? LastStrategy { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    /// <summary>Проверять Zapret UI и Flowseal при запуске (только с подтверждением пользователя).</summary>
    public bool CheckUpdatesOnStartup { get; set; } = true;
    /// <summary>Устарело: авто-установка без подтверждения отключена.</summary>
    public bool AutoUpdateApp { get; set; } = false;
    public string? UpdateManifestUrl { get; set; }
    public string? LastInstalledVersion { get; set; }
    public bool SecuritySetupCompleted { get; set; }
    public bool SecuritySetupSkipped { get; set; }
    /// <summary>ID фона главной страницы (см. HomeBackgroundCatalog).</summary>
    public string HomeBackground { get; set; } = "shooting-stars";

    /// <summary>Скорость анимации фона, 5–100% (ползунок на «Главной»).</summary>
    public int BackgroundSpeedPercent { get; set; } = 15;

    /// <summary>Устарело — миграция в <see cref="BackgroundSpeedPercent"/>.</summary>
    public double BackgroundAnimSpeed { get; set; } = 0.15;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                // Раньше AutoUpdateApp включал установку без спроса — теперь только проверка.
                if (settings.AutoUpdateApp && !settings.CheckUpdatesOnStartup)
                    settings.CheckUpdatesOnStartup = true;
                settings.AutoUpdateApp = false;
                if (settings.BackgroundSpeedPercent is < 5 or > 100)
                    settings.BackgroundSpeedPercent = BackgroundMotion.PercentFromLegacyMultiplier(
                        settings.BackgroundAnimSpeed);
                if (settings.BackgroundSpeedPercent < 8)
                    settings.BackgroundSpeedPercent = 15;
                return settings;
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
