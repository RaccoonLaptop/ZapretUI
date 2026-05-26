using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZapretUI.Services;

public sealed class SecuritySetupService
{
    private readonly ZapretPaths _paths;
    private readonly string _installDir;

    public SecuritySetupService(ZapretPaths paths)
    {
        _paths = paths;
        _installDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
    }

    public SecuritySetupPlan BuildPlan()
    {
        var exclusions = new List<string>();
        var programs = new List<string>();

        if (Directory.Exists(_installDir))
            exclusions.Add(_installDir);

        if (_paths.IsValid)
        {
            exclusions.Add(_paths.Root);
            var winws = Path.Combine(_paths.Bin, "winws.exe");
            if (File.Exists(winws))
                programs.Add(winws);
        }

        var uiExe = Path.Combine(_installDir, "ZapretUI.exe");
        if (File.Exists(uiExe))
            programs.Add(uiExe);

        exclusions = exclusions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        programs = programs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new SecuritySetupPlan(exclusions, programs);
    }

    public async Task<SecurityCheckStatus> CheckStatusAsync(CancellationToken ct = default)
    {
        var plan = BuildPlan();
        var script = ResolveCheckScript();
        if (script is null)
            return SecurityCheckStatus.Unknown("Скрипт проверки не найден");

        var exclusions = string.Join("|", plan.ExclusionPaths);
        var programs = string.Join("|", plan.ProgramPaths);
        var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" " +
                   $"-Exclusions \"{exclusions}\" -Programs \"{programs}\"";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                return SecurityCheckStatus.Unknown("Не удалось запустить проверку");

            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (string.IsNullOrWhiteSpace(output))
                return SecurityCheckStatus.Unknown("Пустой ответ проверки");

            var dto = JsonSerializer.Deserialize<SecurityCheckDto>(output, JsonOptions);
            if (dto is null)
                return SecurityCheckStatus.Unknown("Не удалось разобрать результат");

            return new SecurityCheckStatus
            {
                CheckSucceeded = true,
                DefenderAvailable = dto.DefenderAvailable,
                DefenderAllExcluded = dto.DefenderAllExcluded,
                FirewallAllConfigured = dto.FirewallAllConfigured,
                MissingExclusions = dto.MissingExclusions ?? [],
                MissingFirewallPrograms = dto.MissingFirewallPrograms ?? []
            };
        }
        catch (Exception ex)
        {
            return SecurityCheckStatus.Unknown(ex.Message);
        }
    }

    public async Task<SecuritySetupResult> ApplyAsync(bool addDefender, bool addFirewall, CancellationToken ct = default)
    {
        if (!addDefender && !addFirewall)
            return SecuritySetupResult.Ok("Настройка пропущена");

        var plan = BuildPlan();
        if (plan.ExclusionPaths.Count == 0 && plan.ProgramPaths.Count == 0)
            return SecuritySetupResult.Fail("Не найдены пути для настройки");

        var script = ResolveScript();
        if (script is null)
            return SecuritySetupResult.Fail("Скрипт security-setup.ps1 не найден");

        var logFile = Path.Combine(Path.GetTempPath(), "ZapretUI-security.log");
        var exclusions = addDefender ? string.Join("|", plan.ExclusionPaths) : "";
        var programs = addFirewall ? string.Join("|", plan.ProgramPaths) : "";

        var args = new StringBuilder()
            .Append("-NoProfile -ExecutionPolicy Bypass -File \"")
            .Append(script)
            .Append("\" -Exclusions \"")
            .Append(exclusions)
            .Append("\" -Programs \"")
            .Append(programs)
            .Append("\" -LogFile \"")
            .Append(logFile)
            .Append('"')
            .ToString();

        try
        {
            var tcs = new TaskCompletionSource<int>();
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            if (proc is null)
                return SecuritySetupResult.Fail("Не удалось запустить настройку (UAC отменён?)");

            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
                return SecuritySetupResult.Fail("Настройка завершилась с ошибкой. Проверьте антивирус вручную.");

            var details = File.Exists(logFile) ? await File.ReadAllTextAsync(logFile, ct) : "";
            return SecuritySetupResult.Ok("Исключения и правила брандмауэра применены.", details);
        }
        catch (Exception ex)
        {
            return SecuritySetupResult.Fail(ex.Message);
        }
    }

    private static string? ResolveCheckScript()
    {
        foreach (var c in new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Scripts", "security-check.ps1"),
            Path.Combine(AppContext.BaseDirectory, "security-check.ps1")
        })
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    private static string? ResolveScript()
    {
        foreach (var c in new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Scripts", "security-setup.ps1"),
            Path.Combine(AppContext.BaseDirectory, "security-setup.ps1")
        })
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class SecurityCheckDto
    {
        [JsonPropertyName("defenderAvailable")]
        public bool DefenderAvailable { get; set; }

        [JsonPropertyName("defenderAllExcluded")]
        public bool DefenderAllExcluded { get; set; }

        [JsonPropertyName("missingExclusions")]
        public List<string>? MissingExclusions { get; set; }

        [JsonPropertyName("firewallAllConfigured")]
        public bool FirewallAllConfigured { get; set; }

        [JsonPropertyName("missingFirewallPrograms")]
        public List<string>? MissingFirewallPrograms { get; set; }
    }
}

public sealed record SecuritySetupPlan(IReadOnlyList<string> ExclusionPaths, IReadOnlyList<string> ProgramPaths);

public sealed class SecurityCheckStatus
{
    public bool CheckSucceeded { get; init; }
    public bool DefenderAvailable { get; init; }
    public bool DefenderAllExcluded { get; init; }
    public bool FirewallAllConfigured { get; init; }
    public IReadOnlyList<string> MissingExclusions { get; init; } = [];
    public IReadOnlyList<string> MissingFirewallPrograms { get; init; } = [];
    public string? Error { get; init; }

    public bool IsFullyConfigured =>
        CheckSucceeded &&
        FirewallAllConfigured &&
        (DefenderAllExcluded || !DefenderAvailable);

    public bool HasDefenderIssue => CheckSucceeded && DefenderAvailable && !DefenderAllExcluded;
    public bool HasFirewallIssue => CheckSucceeded && !FirewallAllConfigured;

    public string Summary
    {
        get
        {
            if (!CheckSucceeded)
                return Error ?? "Проверка безопасности недоступна";

            if (IsFullyConfigured)
                return "Антивирус и брандмауэр настроены";

            var parts = new List<string>();
            if (HasDefenderIssue)
                parts.Add("нет исключений в Windows Defender");
            if (!DefenderAvailable)
                parts.Add("Defender недоступен — проверьте сторонний антивирус вручную");
            if (HasFirewallIssue)
                parts.Add("нет правил брандмауэра");
            return string.Join("; ", parts);
        }
    }

    public static SecurityCheckStatus Unknown(string error) => new()
    {
        CheckSucceeded = false,
        Error = error
    };
}

public sealed class SecuritySetupResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? Details { get; init; }

    public static SecuritySetupResult Ok(string msg, string? details = null) =>
        new() { Success = true, Message = msg, Details = details };

    public static SecuritySetupResult Fail(string msg) =>
        new() { Success = false, Message = msg };
}
