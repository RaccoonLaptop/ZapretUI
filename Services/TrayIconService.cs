using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Application = System.Windows.Application;
using DrawingColor = System.Drawing.Color;
using DrawingIcon = System.Drawing.Icon;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPen = System.Drawing.Pen;
using DrawingBrush = System.Drawing.SolidBrush;

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
            Icon = CreateTrayIcon(),
            Text = "Zapret UI",
            Visible = false
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Открыть Zapret UI", null, (_, _) => ShowWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => RequestExit());
        _notifyIcon.ContextMenuStrip = menu;

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
    }

    public void ShowInTray()
    {
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(
            2500,
            "Zapret UI",
            "Обход работает — приложение в трее. Дважды кликните по иконке, чтобы открыть.",
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

    private static DrawingIcon CreateTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
            return new DrawingIcon(iconPath);

        const int size = 32;
        using var bmp = new DrawingBitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(DrawingColor.FromArgb(8, 9, 13));
        using var brush = new DrawingBrush(DrawingColor.FromArgb(107, 159, 255));
        g.FillEllipse(brush, 4, 4, size - 8, size - 8);
        using var pen = new DrawingPen(DrawingColor.FromArgb(232, 235, 244), 2f);
        g.DrawLine(pen, 11, 16, 15, 20);
        g.DrawLine(pen, 15, 20, 22, 12);
        return DrawingIcon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
