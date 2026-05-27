using System.Diagnostics;
using System.IO;
using System.Text;

namespace ZapretUI.Services;

public sealed class NetworkResetService
{
    public static async Task<(bool Success, string Output)> RunAllAsync(CancellationToken ct = default)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "zapret-netreset.log");
        try { if (File.Exists(logPath)) File.Delete(logPath); } catch { /* ignore */ }

        var script = BuildScript(logPath);
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            if (process is null)
                return (false, "Не удалось запустить (нужны права администратора).");

            await process.WaitForExitAsync(ct);

            if (File.Exists(logPath))
            {
                var text = await File.ReadAllTextAsync(logPath, ct);
                return (process.ExitCode == 0, string.IsNullOrWhiteSpace(text)
                    ? "Сброс выполнен. Рекомендуется перезагрузить компьютер."
                    : text.TrimEnd());
            }

            return (process.ExitCode == 0,
                "Сброс выполнен. Рекомендуется перезагрузить компьютер.");
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    private static string BuildScript(string logPath)
    {
        var escapedLog = logPath.Replace("'", "''");
        return $@"
$ErrorActionPreference = 'Continue'
$log = New-Object System.Text.StringBuilder
$steps = @(
    @{{ L='Сброс IP'; C='netsh int ip reset' }},
    @{{ L='Сброс WinHTTP proxy'; C='netsh winhttp reset proxy' }},
    @{{ L='Сброс Winsock'; C='netsh winsock reset' }},
    @{{ L='Сброс IPv4'; C='netsh interface ipv4 reset' }},
    @{{ L='Сброс IPv6'; C='netsh interface ipv6 reset' }},
    @{{ L='Диапазон TCP (10000-30000)'; C='netsh int ipv4 set dynamicport tcp start=10000 num=30000' }},
    @{{ L='Очистка DNS'; C='ipconfig /flushdns' }}
)
foreach ($s in $steps) {{
    [void]$log.AppendLine(""=== $($s.L) ==="")
    [void]$log.AppendLine(""> $($s.C)"")
    $out = cmd /c $($s.C) 2>&1 | Out-String
    if ($out.Trim()) {{ [void]$log.AppendLine($out.TrimEnd()) }} else {{ [void]$log.AppendLine('OK') }}
    [void]$log.AppendLine()
}}
[void]$log.AppendLine('Готово. Перезагрузите компьютер, чтобы применить изменения.')
$text = $log.ToString()
Set-Content -Path '{escapedLog}' -Value $text -Encoding UTF8
Write-Output $text
";
    }
}
