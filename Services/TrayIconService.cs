using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using DFont = System.Drawing.Font;
using DFontStyle = System.Drawing.FontStyle;
using ZapretUI.Helpers;
using Application = System.Windows.Application;

namespace ZapretUI.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly Func<Task> _toggleBypassAsync;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _strategyItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _openItem;
    private readonly ToolStripMenuItem _exitItem;
    private Icon? _idleIcon;
    private Icon? _activeIcon;
    private bool _disposed;
    private bool _running;
    private bool _busy;
    private string? _strategyTitle;

    public TrayIconService(Window window, Func<Task> toggleBypassAsync)
    {
        _window = window;
        _toggleBypassAsync = toggleBypassAsync;

        _idleIcon = TrayIconGenerator.Create(active: false);
        _activeIcon = TrayIconGenerator.Create(active: true);

        _menu = new ContextMenuStrip
        {
            Renderer = new TrayMenuRenderer(),
            ShowImageMargin = false,
            AutoSize = true,
            BackColor = TrayMenuTheme.Bg,
            ForeColor = TrayMenuTheme.Text,
            Padding = new Padding(4, 6, 4, 6),
            MinimumSize = new System.Drawing.Size(240, 0)
        };
        _menu.Opening += (_, _) => ApplyLocalization();

        _statusItem = new ToolStripMenuItem
        {
            AutoSize = true,
            Padding = new Padding(12, 8, 12, 4),
            Font = new DFont("Segoe UI", 9.5f, DFontStyle.Bold),
            ForeColor = TrayMenuTheme.Text,
            Tag = new TrayStatusTag { Running = false }
        };
        _statusItem.Click += (_, _) => { /* status row — no action */ };

        _strategyItem = new ToolStripMenuItem
        {
            AutoSize = true,
            Padding = new Padding(28, 0, 12, 8),
            ForeColor = TrayMenuTheme.TextMuted,
            Font = new DFont("Segoe UI", 8.5f),
            Visible = false
        };
        _strategyItem.Click += (_, _) => { /* info row — no action */ };

        _toggleItem = new ToolStripMenuItem
        {
            Font = new DFont("Segoe UI", 9.25f),
            ForeColor = TrayMenuTheme.Accent,
            Padding = new Padding(12, 6, 12, 6)
        };
        _toggleItem.Click += async (_, _) => await RunToggleAsync();

        _openItem = new ToolStripMenuItem
        {
            Font = new DFont("Segoe UI", 9.25f),
            ForeColor = TrayMenuTheme.Text,
            Padding = new Padding(12, 6, 12, 6)
        };
        _openItem.Click += (_, _) => ShowWindow();

        _exitItem = new ToolStripMenuItem
        {
            Font = new DFont("Segoe UI", 9.25f),
            ForeColor = TrayMenuTheme.Text,
            Padding = new Padding(12, 6, 12, 6)
        };
        _exitItem.Click += (_, _) => RequestExit();

        _menu.Items.Add(_statusItem);
        _menu.Items.Add(_strategyItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_toggleItem);
        _menu.Items.Add(_openItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = Loc.T("app.title"),
            Visible = true,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
        ApplyLocalization();
        UpdateState(false, null, false);
    }

    public void UpdateState(bool running, string? strategyTitle, bool busy)
    {
        _running = running;
        _busy = busy;
        _strategyTitle = strategyTitle;

        _statusItem.Tag = new TrayStatusTag { Running = running };
        _statusItem.Text = busy
            ? Loc.T("tray.status.starting")
            : (running ? Loc.T("status.running") : Loc.T("status.stopped"));

        var hasStrategy = running && !string.IsNullOrWhiteSpace(strategyTitle);
        _strategyItem.Visible = hasStrategy;
        _strategyItem.Text = hasStrategy ? strategyTitle! : "";

        _toggleItem.Text = busy
            ? Loc.T("home.starting")
            : (running ? Loc.T("tray.toggle.stop") : Loc.T("tray.toggle.start"));
        _toggleItem.Enabled = !busy;
        _toggleItem.ForeColor = running ? TrayMenuTheme.Error : TrayMenuTheme.Accent;

        _notifyIcon.Icon = running ? _activeIcon : _idleIcon;
        _notifyIcon.Text = busy
            ? Loc.T("tray.status.starting")
            : (running
                ? Loc.F("tray.tooltip_running", strategyTitle ?? Loc.T("status.running"))
                : Loc.T("tray.tooltip_stopped"));
    }

    private void ApplyLocalization()
    {
        _openItem.Text = Loc.T("tray.open");
        _exitItem.Text = Loc.T("tray.exit");
        UpdateState(_running, _strategyTitle, _busy);
    }

    private async Task RunToggleAsync()
    {
        try
        {
            await _window.Dispatcher.InvokeAsync(async () => await _toggleBypassAsync());
        }
        catch (Exception ex)
        {
            _window.Dispatcher.Invoke(() =>
                UiHelpers.ShowError(ex.Message));
        }
    }

    public void ShowInTray()
    {
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(
            2500,
            Loc.T("app.title"),
            Loc.T("tray.balloon"),
            ToolTipIcon.Info);
    }

    public void HideFromTray()
    {
        // Keep icon in notification area for quick start/stop.
    }

    public void ShowWindow()
    {
        _window.Dispatcher.Invoke(() =>
        {
            _window.Show();
            _window.WindowState = WindowState.Normal;
            _window.Activate();
            _window.Focus();
        });
    }

    public void RequestExit()
    {
        _window.Dispatcher.Invoke(() =>
        {
            if (_window is MainWindow mw)
                mw.ShutdownApplication();
            else
                Application.Current.Shutdown();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _idleIcon?.Dispose();
        _activeIcon?.Dispose();
    }
}
