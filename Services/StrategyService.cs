using System.Diagnostics;
using System.IO;

namespace ZapretUI.Services;

public sealed class StrategyService
{
    private readonly ZapretPaths _paths;
    private readonly ProcessRunner _runner;

    public StrategyService(ZapretPaths paths, ProcessRunner runner)
    {
        _paths = paths;
        _runner = runner;
    }

    public bool IsRunning()
    {
        return Process.GetProcessesByName("winws").Length > 0;
    }

    public string? GetRunningStrategyTitle()
    {
        foreach (var p in Process.GetProcessesByName("winws"))
        {
            try
            {
                var title = p.MainWindowTitle;
                if (title.StartsWith("zapret:", StringComparison.OrdinalIgnoreCase))
                    return title["zapret:".Length..].Trim();
            }
            catch { /* ignore */ }
        }
        return null;
    }

    public async Task StartStrategyAsync(string batFileName, CancellationToken ct = default)
    {
        var batPath = Path.Combine(_paths.Root, batFileName);
        if (!File.Exists(batPath))
            throw new FileNotFoundException("Strategy file not found", batPath);

        if (IsRunning())
            await StopStrategyAsync(ct);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batPath}\"",
            WorkingDirectory = _paths.Root,
            UseShellExecute = true,
            CreateNoWindow = false
        };
        Process.Start(psi);
        await Task.Delay(1500, ct);
    }

    public Task StopStrategyAsync(CancellationToken ct = default)
    {
        foreach (var p in Process.GetProcessesByName("winws"))
        {
            try { p.Kill(true); } catch { /* ignore */ }
        }
        return Task.CompletedTask;
    }

    public string ReadStrategyContent(string batFileName) =>
        File.ReadAllText(Path.Combine(_paths.Root, batFileName));

    public void SaveStrategyContent(string batFileName, string content)
    {
        var path = Path.Combine(_paths.Root, batFileName);
        File.WriteAllText(path, content);
    }

    public string CreateCustomStrategy(string baseStrategy, string newName)
    {
        if (!newName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            newName += ".bat";

        var basePath = Path.Combine(_paths.Root, baseStrategy);
        var newPath = Path.Combine(_paths.Root, newName);
        if (File.Exists(newPath))
            throw new InvalidOperationException("File already exists");

        File.Copy(basePath, newPath);
        return newName;
    }

    public bool CanDeleteStrategy(string batFileName)
    {
        if (batFileName.StartsWith("service", StringComparison.OrdinalIgnoreCase))
            return false;
        if (BundledStrategiesService.IsProtected(batFileName))
            return false;
        return true;
    }

    public void DeleteStrategy(string batFileName)
    {
        if (!CanDeleteStrategy(batFileName))
            throw new InvalidOperationException("Нельзя удалить служебный файл service.bat");

        var path = Path.Combine(_paths.Root, batFileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл не найден", batFileName);

        File.Delete(path);
    }

    public string RenameStrategy(string oldName, string newName)
    {
        if (!newName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            newName += ".bat";

        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            return oldName;

        if (oldName.StartsWith("service", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Нельзя переименовать служебный файл service.bat");

        var oldPath = Path.Combine(_paths.Root, oldName);
        var newPath = Path.Combine(_paths.Root, newName);
        if (!File.Exists(oldPath))
            throw new FileNotFoundException("Файл не найден", oldName);
        if (File.Exists(newPath))
            throw new InvalidOperationException("Файл с таким именем уже существует");

        File.Move(oldPath, newPath);
        return newName;
    }

    public async Task<string> GetServiceStrategyAsync()
    {
        var script = $@"
            $val = (Get-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Services\zapret' -Name 'zapret-discord-youtube' -ErrorAction SilentlyContinue).'zapret-discord-youtube'
            if ($val) {{ Write-Output $val }}
        ";
        var output = new List<string>();
        var runner = new ProcessRunner();
        runner.OutputReceived += output.Add;
        await runner.RunPowerShellAsync(script);
        return output.FirstOrDefault() ?? "";
    }
}
