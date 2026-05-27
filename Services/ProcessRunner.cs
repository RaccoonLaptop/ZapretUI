using System.Diagnostics;
using System.Text;

namespace ZapretUI.Services;

public sealed class ProcessRunner
{
    private string? _zapretRoot;

    public event Action<string>? OutputReceived;

    public void SetZapretRoot(string root) => _zapretRoot = root;

    public async Task<int> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) Emit(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) Emit(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    public async Task<int> RunPowerShellAsync(string script, string? workingDirectory = null, CancellationToken ct = default)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        return await RunAsync("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
            workingDirectory, ct);
    }

    public async Task<string> RunBridgeAsync(string action, string? extra = null, CancellationToken ct = default)
    {
        if (action == "RunTests")
        {
            RunInteractiveTest();
            return "Открыто окно тестирования стратегий.";
        }

        var scriptPath = ResolveBridgeScript();
        var root = _zapretRoot ?? ZapretPaths.DetectRoot();
        var needsAdmin = action is "InstallService" or "RemoveServices";

        var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Action \"{action}\" -Root \"{root}\"";
        if (!string.IsNullOrEmpty(extra))
            args += $" -Extra \"{extra.Replace("\"", "`\"")}\"";

        var captured = new StringBuilder();

        if (needsAdmin)
        {
            Emit("Требуются права администратора — подтвердите UAC...");
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = false
            };
            using var process = Process.Start(psi);
            if (process is not null)
                await process.WaitForExitAsync(ct);
            var msg = $"--- {action} завершено (окно администратора) ---";
            Emit(msg);
            return msg;
        }

        void Capture(string line) => captured.AppendLine(line);

        OutputReceived += Capture;
        try
        {
            await RunAsync("powershell.exe", args, root, ct);
        }
        finally
        {
            OutputReceived -= Capture;
        }

        return captured.Length > 0 ? captured.ToString().TrimEnd() : "Операция завершена.";
    }

    public Task RunInteractiveTestAsync(CancellationToken ct = default)
    {
        RunInteractiveTest();
        return Task.CompletedTask;
    }

    private void RunInteractiveTest()
    {
        var root = _zapretRoot ?? ZapretPaths.DetectRoot();
        var testScript = ResolveTestScript(root);
        if (testScript is null)
            throw new FileNotFoundException(
                "Скрипт test zapret.ps1 не найден в папке zapret\\utils.\n" +
                "Обновите компоненты Flowseal через «Сервис → Обновления» или переустановите программу.");

        Emit("Открывается окно тестирования стратегий...");
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -NoExit -File \"{testScript}\"",
            WorkingDirectory = root,
            UseShellExecute = true
        });
    }

    private static string? ResolveTestScript(string root)
    {
        var utils = Path.Combine(root, "utils");
        if (!Directory.Exists(utils)) return null;

        var exact = Path.Combine(utils, "test zapret.ps1");
        if (File.Exists(exact)) return exact;

        foreach (var file in Directory.GetFiles(utils, "test*.ps1"))
        {
            if (file.Contains("test", StringComparison.OrdinalIgnoreCase))
                return file;
        }

        return null;
    }

    private static string ResolveBridgeScript()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Scripts", "ui-bridge.ps1"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Scripts", "ui-bridge.ps1"),
            Path.Combine(AppContext.BaseDirectory, "..", "ZapretUI", "Scripts", "ui-bridge.ps1"),
            Path.Combine(ZapretPaths.DetectRoot(), "ZapretUI-Program", "Scripts", "ui-bridge.ps1"),
            Path.Combine(ZapretPaths.DetectRoot(), "ZapretUI", "Scripts", "ui-bridge.ps1")
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }

        throw new FileNotFoundException("ui-bridge.ps1 not found");
    }

    private void Emit(string line) => OutputReceived?.Invoke(line);
}
