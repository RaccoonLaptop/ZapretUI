namespace ZapretUI.Services;

public static class ZapretHealth
{
    public const string GuideUrl = "https://raccoonlaptop.github.io/ZapretUI/";

    public static IReadOnlyList<string> MissingFiles(ZapretPaths paths)
    {
        var missing = new List<string>();
        if (!File.Exists(Path.Combine(paths.Bin, "winws.exe")))
            missing.Add("winws.exe");
        if (!File.Exists(Path.Combine(paths.Bin, "WinDivert.dll")))
            missing.Add("WinDivert.dll");
        if (!File.Exists(Path.Combine(paths.Bin, "WinDivert64.sys")))
            missing.Add("WinDivert64.sys");
        return missing;
    }
}
