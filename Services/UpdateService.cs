using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ZapretUI.Services;

public sealed class UpdateService
{
    private const string VersionUrl = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/main/.service/version.txt";
    private const string ReleaseUrl = "https://github.com/Flowseal/zapret-discord-youtube/releases/latest";

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly ZapretPaths _paths;

    public UpdateService(ZapretPaths paths) => _paths = paths;

    public string LocalVersion => _paths.GetLocalVersion();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, VersionUrl);
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var remote = (await response.Content.ReadAsStringAsync(ct)).Trim();
            var local = LocalVersion;
            return new UpdateCheckResult
            {
                LocalVersion = local,
                RemoteVersion = remote,
                IsUpToDate = string.Equals(local, remote, StringComparison.OrdinalIgnoreCase),
                ReleaseUrl = ReleaseUrl
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult { Error = ex.Message, LocalVersion = LocalVersion };
        }
    }

    public async Task<bool> UpdateIpsetListAsync(CancellationToken ct = default)
    {
        var url = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/ipset-service.txt";
        var listFile = Path.Combine(_paths.Lists, "ipset-all.txt");
        var content = await _http.GetStringAsync(url, ct);
        Directory.CreateDirectory(_paths.Lists);
        await File.WriteAllTextAsync(listFile, content, ct);
        return true;
    }

    public void OpenReleasePage() =>
        Process.Start(new ProcessStartInfo(ReleaseUrl) { UseShellExecute = true });
}

public sealed class UpdateCheckResult
{
    public string LocalVersion { get; init; } = "";
    public string RemoteVersion { get; init; } = "";
    public bool IsUpToDate { get; init; }
    public string ReleaseUrl { get; init; } = "";
    public string? Error { get; init; }
}
