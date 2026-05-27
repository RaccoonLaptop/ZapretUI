using System.Windows;
using System.Windows.Forms;
using ZapretUI.Helpers;
using Application = System.Windows.Application;

namespace ZapretUI.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _window;
    private bool _disposed;

    public TrayIconService(Window window)
    {
        _window = window;

        _notifyIcon = new NotifyIcon
        {
            Icon = (System.Drawing.Icon)AppIcon.DrawingIcon.Clone(),
            Text = "Zapret UI",
            Visible = false
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(Loc.T("tray.open"), null, (_, _) => ShowWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Loc.T("tray.exit"), null, (_, _) => RequestExit());
        _notifyIcon.ContextMenuStrip = menu;

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
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
        _notifyIcon.Visible = false;
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
        HideFromTray();
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
    }
}
