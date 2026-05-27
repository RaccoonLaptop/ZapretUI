using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZapretUI.Services;

public sealed class AppSelfUpdateService
{
    // URL манифеста обновлений (можно переопределить в settings.json)
    public const string DefaultManifestUrl =
        "https://raw.githubusercontent.com/RaccoonLaptop/ZapretUI/main/update.json";

    private const string GitHubReleasesApi =
        "https://api.github.com/repos/RaccoonLaptop/ZapretUI/releases/latest";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };
    private readonly AppSettings _settings;
    private readonly string _installDir;
    private readonly string? _zapretRoot;

    public AppSelfUpdateService(AppSettings settings, string? zapretRoot = null)
    {
        _settings = settings;
        _installDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        _zapretRoot = zapretRoot;
    }

    public static string GetLocalVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+');
            return plus > 0 ? info[..plus] : info;
        }
        return asm.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    public async Task<AppUpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
    {
        var local = GetLocalVersion();
        try
        {
            var manifest = await LoadManifestAsync(ct);
            if (manifest is null)
            {
                return new AppUpdateCheckResult
                {
                    LocalVersion = local,
                    IsUpToDate = true,
                    Message = "Манифест обновлений не найден"
                };
            }

            var remote = manifest.Version.Trim();
            var hasUpdate = IsNewerVersion(remote, local);
            return new AppUpdateCheckResult
            {
                LocalVersion = local,
                RemoteVersion = remote,
                IsUpToDate = !hasUpdate,
                HasUpdate = hasUpdate,
                Manifest = manifest,
                Message = hasUpdate
                    ? $"Доступна версия {remote}"
                    : $"Zapret UI актуален ({local})"
            };
        }
        catch (Exception ex)
        {
            return new AppUpdateCheckResult
            {
                LocalVersion = local,
                IsUpToDate = true,
                Error = ex.Message
            };
        }
    }

    public async Task<AppUpdatePrepareResult> PrepareUpdateAsync(AppUpdateManifest manifest, CancellationToken ct = default)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ZapretUI-update-" + Guid.NewGuid().ToString("N"));
        var extractDir = Path.Combine(tempRoot, "extract");

        try
        {
            Directory.CreateDirectory(extractDir);

            var packagePath = await ResolvePackagePathAsync(manifest, tempRoot, ct);
            if (packagePath is null)
                return AppUpdatePrepareResult.Fail("Файл обновления не найден");

            if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(packagePath, extractDir, overwriteFiles: true);
            }
            else if (Directory.Exists(packagePath))
            {
                CopyDirectory(packagePath, extractDir);
            }
            else
            {
                return AppUpdatePrepareResult.Fail("Неподдерживаемый формат пакета");
            }

            var sourceDir = FindProgramRoot(extractDir);
            if (sourceDir is null || !File.Exists(Path.Combine(sourceDir, "ZapretUI.exe")))
                return AppUpdatePrepareResult.Fail("В пакете нет ZapretUI.exe");

            return AppUpdatePrepareResult.Ok(new PreparedAppUpdate
            {
                TempRoot = tempRoot,
                SourceDir = sourceDir,
                ManifestVersion = manifest.Version
            });
        }
        catch (Exception ex)
        {
            try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
            return AppUpdatePrepareResult.Fail(ex.Message);
        }
    }

    public async Task<AppUpdateInstallResult> InstallPreparedUpdateAsync(PreparedAppUpdate prepared, CancellationToken ct = default)
    {
        try
        {
            var scriptPath = ResolveUpdateScript();
            if (scriptPath is null)
                return AppUpdateInstallResult.Fail("Скрипт apply-update.ps1 не найден");

            var logFile = Path.Combine(prepared.TempRoot, "update.log");
            var exePath = Path.Combine(_installDir, "ZapretUI.exe");
            var pid = Process.GetCurrentProcess().Id;
            var args = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\" " +
                       $"-SourceDir \"{prepared.SourceDir}\" -TargetDir \"{_installDir}\" -ProcessId {pid} " +
                       $"-ExePath \"{exePath}\" -LogFile \"{logFile}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            _settings.LastInstalledVersion = prepared.ManifestVersion;
            _settings.Save();
            return AppUpdateInstallResult.Ok("Обновление запускается, программа перезапустится...", restart: true);
        }
        catch (Exception ex)
        {
            return AppUpdateInstallResult.Fail(ex.Message);
        }
    }

    public async Task<AppUpdateInstallResult> InstallUpdateAsync(AppUpdateManifest manifest, CancellationToken ct = default)
    {
        var prepared = await PrepareUpdateAsync(manifest, ct);
        if (!prepared.Success || prepared.Payload is null)
            return AppUpdateInstallResult.Fail(prepared.Message);
        return await InstallPreparedUpdateAsync(prepared.Payload, ct);
    }

    public static void CleanupPreparedUpdate(PreparedAppUpdate? prepared)
    {
        if (prepared is null) return;
        try
        {
            if (Directory.Exists(prepared.TempRoot))
                Directory.Delete(prepared.TempRoot, true);
        }
        catch { /* ignore */ }
    }

    public async Task<AppUpdateInstallResult> CheckAndInstallIfNeededAsync(bool autoInstall, CancellationToken ct = default)
    {
        var check = await CheckForUpdateAsync(ct);
        if (check.Error is not null)
            return AppUpdateInstallResult.Fail(check.Error);

        if (!check.HasUpdate || check.Manifest is null)
            return AppUpdateInstallResult.Ok(check.Message ?? "Обновление не требуется");

        if (!Helpers.UiHelpers.Confirm(
                $"Доступна новая версия Zapret UI: {check.RemoteVersion} (у вас {check.LocalVersion}).\n\nУстановить сейчас?"))
        {
            return AppUpdateInstallResult.Ok("Обновление отложено");
        }

        return await InstallUpdateAsync(check.Manifest, ct);
    }

    private async Task<AppUpdateManifest?> LoadManifestAsync(CancellationToken ct)
    {
        var candidates = new List<AppUpdateManifest>();

        foreach (var url in GetRemoteManifestUrls())
        {
            var manifest = await TryLoadManifestUrlAsync(url, ct);
            if (manifest is not null)
                candidates.Add(manifest);
        }

        var fromRelease = await TryLoadManifestFromGitHubReleaseAsync(ct);
        if (fromRelease is not null)
            candidates.Add(fromRelease);

        if (candidates.Count > 0)
            return PickNewestManifest(candidates);

        foreach (var path in GetLocalManifestPaths())
        {
            var manifest = await TryLoadManifestFileAsync(path, ct);
            if (manifest is not null)
                return manifest;
        }

        return null;
    }

    private IEnumerable<string> GetRemoteManifestUrls()
    {
        if (!string.IsNullOrWhiteSpace(_settings.UpdateManifestUrl))
            yield return _settings.UpdateManifestUrl.Trim();

        yield return DefaultManifestUrl;
    }

    private IEnumerable<string> GetLocalManifestPaths()
    {
        yield return Path.Combine(_installDir, "update.json");

        if (!string.IsNullOrEmpty(_zapretRoot))
            yield return Path.Combine(_zapretRoot, "ZapretUI-update", "update.json");
    }

    private async Task<AppUpdateManifest?> TryLoadManifestUrlAsync(string url, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            req.Headers.TryAddWithoutValidation("Pragma", "no-cache");
            var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync(ct);
            return ParseManifest(json, "");
        }
        catch
        {
            return null;
        }
    }

    private async Task<AppUpdateManifest?> TryLoadManifestFileAsync(string path, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path, ct);
            return ParseManifest(json, Path.GetDirectoryName(path)!);
        }
        catch
        {
            return null;
        }
    }

    private async Task<AppUpdateManifest?> TryLoadManifestFromGitHubReleaseAsync(CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, GitHubReleasesApi);
            req.Headers.UserAgent.ParseAdd("ZapretUI-Updater");
            req.Headers.Accept.ParseAdd("application/vnd.github+json");
            var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.Trim().TrimStart('v') ?? "";
            if (string.IsNullOrEmpty(tag)) return null;

            string? zipUrl = null;
            string? setupUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    var url = asset.GetProperty("browser_download_url").GetString();
                    if (string.IsNullOrEmpty(url)) continue;
                    if (name.Equals("ZapretUI-Program.zip", StringComparison.OrdinalIgnoreCase))
                        zipUrl = url;
                    if (name.Equals("ZapretUI-Setup.exe", StringComparison.OrdinalIgnoreCase))
                        setupUrl = url;
                }
            }

            return new AppUpdateManifest
            {
                Version = tag,
                DownloadUrl = zipUrl,
                InstallerUrl = setupUrl,
                BaseDirectory = ""
            };
        }
        catch
        {
            return null;
        }
    }

    private static AppUpdateManifest? ParseManifest(string json, string baseDirectory)
    {
        var manifest = JsonSerializer.Deserialize<AppUpdateManifest>(json, JsonOptions);
        if (manifest is null) return null;
        manifest.BaseDirectory = baseDirectory;
        return manifest;
    }

    private static AppUpdateManifest PickNewestManifest(IReadOnlyList<AppUpdateManifest> manifests)
    {
        AppUpdateManifest? best = null;
        Version? bestVersion = null;

        foreach (var m in manifests)
        {
            if (!Version.TryParse(NormalizeVersion(m.Version), out var v))
                continue;
            if (bestVersion is null || v > bestVersion)
            {
                bestVersion = v;
                best = m;
            }
        }

        return best ?? manifests[0];
    }

    private async Task<string?> ResolvePackagePathAsync(AppUpdateManifest manifest, string tempRoot, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(manifest.PackageFile))
        {
            var local = Path.IsPathRooted(manifest.PackageFile)
                ? manifest.PackageFile
                : Path.Combine(manifest.BaseDirectory, manifest.PackageFile);

            if (File.Exists(local))
            {
                var dest = Path.Combine(tempRoot, Path.GetFileName(local));
                File.Copy(local, dest, true);
                return dest;
            }
        }

        if (!string.IsNullOrWhiteSpace(manifest.DownloadUrl))
        {
            var url = manifest.DownloadUrl.Trim();
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                var path = new Uri(url).LocalPath;
                return File.Exists(path) ? path : null;
            }

            var zipPath = Path.Combine(tempRoot, "package.zip");
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using var fs = File.Create(zipPath);
            await response.Content.CopyToAsync(fs, ct);
            return zipPath;
        }

        return null;
    }

    private static string? FindProgramRoot(string extractDir)
    {
        if (File.Exists(Path.Combine(extractDir, "ZapretUI.exe")))
            return extractDir;

        foreach (var dir in Directory.GetDirectories(extractDir))
        {
            if (File.Exists(Path.Combine(dir, "ZapretUI.exe")))
                return dir;
        }
        return null;
    }

    private static string? ResolveUpdateScript()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Scripts", "apply-update.ps1"),
            Path.Combine(AppContext.BaseDirectory, "apply-update.ps1")
        };
        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    private static bool IsNewerVersion(string remote, string local)
    {
        if (Version.TryParse(NormalizeVersion(remote), out var r) &&
            Version.TryParse(NormalizeVersion(local), out var l))
            return r > l;
        return !string.Equals(remote, local, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string v)
    {
        var parts = v.Trim().Split('.');
        var list = parts.ToList();
        while (list.Count < 3) list.Add("0");
        return string.Join('.', list.Take(3));
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}

public sealed class AppUpdateManifest
{
    public string Version { get; set; } = "";
    public string? DownloadUrl { get; set; }
    public string? InstallerUrl { get; set; }
    public string? PackageFile { get; set; }

    [JsonIgnore] public string BaseDirectory { get; set; } = "";
}

public sealed class AppUpdateCheckResult
{
    public string LocalVersion { get; init; } = "";
    public string RemoteVersion { get; init; } = "";
    public bool IsUpToDate { get; init; }
    public bool HasUpdate { get; init; }
    public AppUpdateManifest? Manifest { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}

public sealed class AppUpdateInstallResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public bool RequiresRestart { get; init; }

    public static AppUpdateInstallResult Ok(string msg, bool restart = false) =>
        new() { Success = true, Message = msg, RequiresRestart = restart };

    public static AppUpdateInstallResult Fail(string msg) =>
        new() { Success = false, Message = msg };
}

public sealed class AppUpdatePrepareResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public PreparedAppUpdate? Payload { get; init; }

    public static AppUpdatePrepareResult Ok(PreparedAppUpdate payload) =>
        new() { Success = true, Message = "Пакет обновления загружен", Payload = payload };

    public static AppUpdatePrepareResult Fail(string msg) =>
        new() { Success = false, Message = msg };
}

public sealed class PreparedAppUpdate
{
    public string TempRoot { get; init; } = "";
    public string SourceDir { get; init; } = "";
    public string ManifestVersion { get; init; } = "";
}
