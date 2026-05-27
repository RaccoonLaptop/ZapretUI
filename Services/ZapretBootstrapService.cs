using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZapretUI.Services;

public sealed class ZapretBootstrapService
{
    private const string ReleasesApi =
        "https://api.github.com/repos/Flowseal/zapret-discord-youtube/releases/latest";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMinutes(15)
    };

    public ZapretBootstrapService()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZapretUI", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<BootstrapResult> EnsureInstalledAsync(
        string targetDir,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            if (ZapretPaths.IsValidZapretRoot(targetDir))
            {
                return BootstrapResult.Ok("Компоненты zapret уже установлены.", targetDir);
            }

            progress?.Report("Получение информации о последней версии...");
            var release = await GetLatestReleaseAsync(ct);
            var asset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            if (asset is null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
                return BootstrapResult.Fail("Не найден zip-архив в последнем релизе Flowseal.");

            var tempRoot = Path.Combine(Path.GetTempPath(), "zapret-bootstrap-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var zipPath = Path.Combine(tempRoot, asset.Name);
                progress?.Report($"Скачивание {asset.Name} ({release.TagName})...");
                await DownloadFileAsync(asset.DownloadUrl, zipPath, progress, ct);

                var extractDir = Path.Combine(tempRoot, "extract");
                progress?.Report("Распаковка...");
                ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

                foreach (var file in Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    var rel = Path.GetRelativePath(extractDir, file);
                    var dest = Path.Combine(targetDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(file, dest, true);
                }

                if (!ZapretPaths.IsValidZapretRoot(targetDir))
                    return BootstrapResult.Fail("Скачанный пакет повреждён или неполный.");

                BundledStrategiesService.DeployTo(targetDir);
                try
                {
                    new ServiceDefaultsService(new ZapretPaths(targetDir)).ApplyFreshInstallDefaults();
                }
                catch { /* ignore */ }
                return BootstrapResult.Ok($"Установлено: Flowseal {release.TagName}", targetDir);
            }
            finally
            {
                try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
            }
        }
        catch (OperationCanceledException)
        {
            return BootstrapResult.Fail("Загрузка отменена.");
        }
        catch (Exception ex)
        {
            return BootstrapResult.Fail(ex.Message);
        }
    }

    private async Task DownloadFileAsync(
        string url,
        string destPath,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(destPath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
            if (total > 0)
            {
                var pct = (int)(readTotal * 100 / total);
                progress?.Report($"Скачивание... {pct}%");
            }
        }
    }

    private async Task<GitHubRelease> GetLatestReleaseAsync(CancellationToken ct)
    {
        var json = await _http.GetStringAsync(ReleasesApi, ct);
        var release = JsonSerializer.Deserialize<GitHubRelease>(json, JsonOptions)
                      ?? throw new InvalidOperationException("Invalid GitHub API response.");
        if (string.IsNullOrWhiteSpace(release.TagName))
            throw new InvalidOperationException("Release tag not found.");
        return release;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; } = "";
    }
}

public sealed class BootstrapResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? InstallPath { get; init; }

    public static BootstrapResult Ok(string message, string path) =>
        new() { Success = true, Message = message, InstallPath = path };

    public static BootstrapResult Fail(string message) =>
        new() { Success = false, Message = message };
}
