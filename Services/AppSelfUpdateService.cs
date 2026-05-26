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

    public async Task<AppUpdateInstallResult> InstallUpdateAsync(AppUpdateManifest manifest, CancellationToken ct = default)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ZapretUI-update-" + Guid.NewGuid().ToString("N"));
        var extractDir = Path.Combine(tempRoot, "extract");
        var logFile = Path.Combine(tempRoot, "update.log");

        try
        {
            Directory.CreateDirectory(extractDir);

            var packagePath = await ResolvePackagePathAsync(manifest, tempRoot, ct);
            if (packagePath is null)
                return AppUpdateInstallResult.Fail("Файл обновления не найден");

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
                return AppUpdateInstallResult.Fail("Неподдерживаемый формат пакета");
            }

            var sourceDir = FindProgramRoot(extractDir);
            if (sourceDir is null || !File.Exists(Path.Combine(sourceDir, "ZapretUI.exe")))
                return AppUpdateInstallResult.Fail("В пакете нет ZapretUI.exe");

            var scriptPath = ResolveUpdateScript();
            if (scriptPath is null)
                return AppUpdateInstallResult.Fail("Скрипт apply-update.ps1 не найден");

            var exePath = Path.Combine(_installDir, "ZapretUI.exe");
            var pid = Process.GetCurrentProcess().Id;

            var args = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\" " +
                       $"-SourceDir \"{sourceDir}\" -TargetDir \"{_installDir}\" -ProcessId {pid} " +
                       $"-ExePath \"{exePath}\" -LogFile \"{logFile}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            _settings.LastInstalledVersion = manifest.Version;
            _settings.Save();

            return AppUpdateInstallResult.Ok("Обновление запускается, программа перезапустится...", restart: true);
        }
        catch (Exception ex)
        {
            try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
            return AppUpdateInstallResult.Fail(ex.Message);
        }
    }

    public async Task<AppUpdateInstallResult> CheckAndInstallIfNeededAsync(bool autoInstall, CancellationToken ct = default)
    {
        var check = await CheckForUpdateAsync(ct);
        if (check.Error is not null)
            return AppUpdateInstallResult.Fail(check.Error);

        if (!check.HasUpdate || check.Manifest is null)
            return AppUpdateInstallResult.Ok(check.Message ?? "Обновление не требуется");

        if (!autoInstall && !Helpers.UiHelpers.Confirm(
                $"Доступна новая версия Zapret UI: {check.RemoteVersion} (у вас {check.LocalVersion}).\n\nУстановить сейчас?"))
        {
            return AppUpdateInstallResult.Ok("Обновление отложено");
        }

        return await InstallUpdateAsync(check.Manifest, ct);
    }

    private async Task<AppUpdateManifest?> LoadManifestAsync(CancellationToken ct)
    {
        foreach (var source in GetManifestSources())
        {
            try
            {
                if (source.IsFile && File.Exists(source.Path))
                {
                    var json = await File.ReadAllTextAsync(source.Path, ct);
                    var manifest = JsonSerializer.Deserialize<AppUpdateManifest>(json, JsonOptions);
                    if (manifest is not null)
                    {
                        manifest.BaseDirectory = Path.GetDirectoryName(source.Path)!;
                        return manifest;
                    }
                }
                else if (source.IsUrl)
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, source.Path);
                    req.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
                    var res = await _http.SendAsync(req, ct);
                    if (!res.IsSuccessStatusCode) continue;
                    var json = await res.Content.ReadAsStringAsync(ct);
                    var manifest = JsonSerializer.Deserialize<AppUpdateManifest>(json, JsonOptions);
                    if (manifest is not null)
                    {
                        manifest.BaseDirectory = Path.GetDirectoryName(source.Path) ?? "";
                        return manifest;
                    }
                }
            }
            catch { /* try next source */ }
        }
        return null;
    }

    private IEnumerable<(string Path, bool IsFile, bool IsUrl)> GetManifestSources()
    {
        var localInstall = Path.Combine(_installDir, "update.json");
        yield return (localInstall, true, false);

        if (!string.IsNullOrEmpty(_zapretRoot))
        {
            var localBundle = Path.Combine(_zapretRoot, "ZapretUI-update", "update.json");
            yield return (localBundle, true, false);
        }

        if (!string.IsNullOrWhiteSpace(_settings.UpdateManifestUrl))
            yield return (_settings.UpdateManifestUrl.Trim(), false, true);
        else
            yield return (DefaultManifestUrl, false, true);
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
