using System.IO;
using System.Text;
using Porta.Pty;

namespace ZapretUI.Services;

public sealed class EmbeddedTerminalHost : IAsyncDisposable
{
    private IPtyConnection? _connection;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    public event Action<string>? OutputReceived;
    public event Action<int>? ProcessExited;
    public event Action<string>? ErrorOccurred;

    public bool IsRunning => _connection is not null;

    public async Task StartPowerShellScriptAsync(string scriptPath, string workingDirectory, CancellationToken ct = default)
    {
        await StopAsync();

        var ps = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(ps))
            ps = "powershell.exe";

        _connection = await SpawnWithFallbackAsync(ps, scriptPath, workingDirectory, ct);
        _connection.ProcessExited += (_, e) =>
        {
            ProcessExited?.Invoke(e.ExitCode);
            _connection = null;
        };

        _readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token), _readCts.Token);
    }

    public async Task WriteInputAsync(string text, CancellationToken ct = default)
    {
        if (_connection is null) return;
        var payload = string.IsNullOrEmpty(text)
            ? "\r"
            : (text.EndsWith('\r') ? text : text + "\r");
        var bytes = Encoding.UTF8.GetBytes(payload);
        await _connection.WriterStream.WriteAsync(bytes, ct);
        await _connection.WriterStream.FlushAsync(ct);
    }

    public async Task SendKeyAsync(char key, CancellationToken ct = default)
    {
        if (_connection is null) return;
        var bytes = Encoding.UTF8.GetBytes(key.ToString());
        await _connection.WriterStream.WriteAsync(bytes, ct);
        await _connection.WriterStream.FlushAsync(ct);
    }

    public async Task StopAsync()
    {
        _readCts?.Cancel();
        if (_readTask is not null)
        {
            try { await _readTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            _readTask = null;
        }
        _readCts?.Dispose();
        _readCts = null;

        if (_connection is not null)
        {
            try { _connection.Kill(); }
            catch { /* ignore */ }
            _connection.Dispose();
            _connection = null;
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (!ct.IsCancellationRequested && _connection is not null)
        {
            int read;
            try
            {
                read = await _connection.ReaderStream.ReadAsync(buffer, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex.Message);
                break;
            }

            if (read <= 0) break;

            var chunk = Encoding.UTF8.GetString(buffer, 0, read);
            if (!string.IsNullOrEmpty(chunk))
            {
                try
                {
                    OutputReceived?.Invoke(chunk);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex.Message);
                }
            }
        }
    }

    private static async Task<IPtyConnection> SpawnWithFallbackAsync(
        string ps,
        string scriptPath,
        string workingDirectory,
        CancellationToken ct)
    {
        var cols = new[] { 300, 260, 200, 120 };
        Exception? last = null;

        foreach (var col in cols)
        {
            try
            {
                return await PtyProvider.SpawnAsync(CreateOptions(ps, scriptPath, workingDirectory, col), ct);
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw last ?? new InvalidOperationException("Failed to start terminal");
    }

    private static PtyOptions CreateOptions(string ps, string scriptPath, string workingDirectory, int cols) =>
        new()
        {
            Name = "ZapretTest",
            Cols = cols,
            Rows = 40,
            Cwd = workingDirectory,
            App = ps,
            CommandLine =
            [
                "-NoLogo",
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", scriptPath
            ]
        };

    public async ValueTask DisposeAsync() => await StopAsync();
}
