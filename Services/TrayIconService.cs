using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using ZapretUI.Helpers;
using Application = System.Windows.Application;

namespace ZapretUI.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly Func<Task> _toggleBypassAsync;
    private readonly NotifyIcon _notifyIcon;
    private TrayMenuPopup? _popup;
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

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = Loc.T("app.title"),
            Visible = true
        };

        _notifyIcon.MouseClick += OnNotifyIconMouseClick;
        _notifyIcon.DoubleClick += (_, _) => ShowWindow();

        UpdateState(false, null, false);
    }

    private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
            ShowTrayMenu();
    }

    private void ShowTrayMenu()
    {
        _window.Dispatcher.Invoke(() =>
        {
            _popup ??= new TrayMenuPopup(
                async () => await _window.Dispatcher.InvokeAsync(async () => await _toggleBypassAsync()),
                ShowWindow,
                RequestExit);

            _popup.UpdateState(_running, _strategyTitle, _busy);
            _popup.ShowNearCursor();
        });
    }

    public void UpdateState(bool running, string? strategyTitle, bool busy)
    {
        _running = running;
        _busy = busy;
        _strategyTitle = strategyTitle;

        _notifyIcon.Icon = running ? _activeIcon : _idleIcon;
        _notifyIcon.Text = busy
            ? Loc.T("tray.status.starting")
            : (running
                ? Loc.F("tray.tooltip_running", strategyTitle ?? Loc.T("status.running"))
                : Loc.T("tray.tooltip_stopped"));

        if (_popup is { IsVisible: true })
            _popup.UpdateState(running, strategyTitle, busy);
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

    public void HideFromTray() { }

    public void ShowWindow()
    {
        _window.Dispatcher.Invoke(() =>
        {
            _popup?.Hide();
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
            _popup?.Hide();
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
        _notifyIcon.MouseClick -= OnNotifyIconMouseClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _popup?.Close();
        _idleIcon?.Dispose();
        _activeIcon?.Dispose();
    }
}
