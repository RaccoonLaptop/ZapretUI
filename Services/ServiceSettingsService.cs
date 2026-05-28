using System.IO;

namespace ZapretUI.Services;

public sealed class ServiceSettingsService
{
    private readonly ZapretPaths _paths;

    public ServiceSettingsService(ZapretPaths paths) => _paths = paths;

    public string GetGameFilterMode()
    {
        var flag = Path.Combine(_paths.Utils, "game_filter.enabled");
        if (!File.Exists(flag)) return "disabled";

        var mode = File.ReadAllText(flag).Trim().ToLowerInvariant();
        return mode switch
        {
            "tcp" => "tcp",
            "udp" => "udp",
            _ => "all"
        };
    }

    public string GetGameFilterStatus()
    {
        return GetGameFilterMode() switch
        {
            "disabled" => "disabled",
            "all" => "enabled (TCP and UDP)",
            "tcp" => "enabled (TCP)",
            "udp" => "enabled (UDP)",
            _ => "enabled"
        };
    }

    public void SetGameFilter(string mode)
    {
        var flag = Path.Combine(_paths.Utils, "game_filter.enabled");
        Directory.CreateDirectory(_paths.Utils);

        if (mode == "disabled")
        {
            if (File.Exists(flag)) File.Delete(flag);
            return;
        }

        File.WriteAllText(flag, mode);
    }

    public string GetIpsetStatus()
    {
        var listFile = Path.Combine(_paths.Lists, "ipset-all.txt");
        if (!File.Exists(listFile)) return "none";

        var lines = File.ReadAllLines(listFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length == 0) return "any";
        if (lines.Any(l => l.Trim() == "203.0.113.113/32")) return "none";
        return "loaded";
    }

    public void SetIpsetFilter(string mode)
    {
        var listFile = Path.Combine(_paths.Lists, "ipset-all.txt");
        var backupFile = listFile + ".backup";
        Directory.CreateDirectory(_paths.Lists);

        var current = GetIpsetStatus();
        if (current == mode.ToLowerInvariant())
            return;

        switch (mode.ToLowerInvariant())
        {
            case "none":
                if (current == "loaded" && File.Exists(listFile))
                {
                    if (File.Exists(backupFile)) File.Delete(backupFile);
                    File.Move(listFile, backupFile);
                }
                File.WriteAllText(listFile, "203.0.113.113/32" + Environment.NewLine);
                break;
            case "any":
                if (current == "loaded" && File.Exists(listFile))
                {
                    if (File.Exists(backupFile)) File.Delete(backupFile);
                    File.Move(listFile, backupFile);
                }
                File.WriteAllText(listFile, "");
                break;
            case "loaded":
                if (File.Exists(backupFile))
                {
                    if (File.Exists(listFile)) File.Delete(listFile);
                    File.Move(backupFile, listFile);
                }
                else
                    throw new InvalidOperationException("No backup to restore. Update IPSet list first.");
                break;
            default:
                throw new ArgumentException($"Unknown IPSet mode: {mode}", nameof(mode));
        }
    }

    public bool IsAutoUpdateEnabled() =>
        File.Exists(Path.Combine(_paths.Utils, "check_updates.enabled"));

    public void ToggleAutoUpdate()
    {
        var flag = Path.Combine(_paths.Utils, "check_updates.enabled");
        Directory.CreateDirectory(_paths.Utils);
        if (File.Exists(flag))
            File.Delete(flag);
        else
            File.WriteAllText(flag, "ENABLED");
    }
}
