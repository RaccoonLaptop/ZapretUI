using System.Windows;
using ZapretUI.Services;

namespace ZapretUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Zapret UI — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown();
        };

        var settings = AppSettings.Load();
        if (!EnsureZapretInstalled(settings))
        {
            Shutdown();
            return;
        }

        var main = new MainWindow();
        MainWindow = main;
        main.Show();
    }

    private static bool EnsureZapretInstalled(AppSettings settings)
    {
        var root = ZapretPaths.DetectRoot(settings.ZapretRoot);
        if (ZapretPaths.IsValidZapretRoot(root))
        {
            settings.ZapretRoot = root;
            settings.Save();
            return true;
        }

        var target = ZapretPaths.GetBundledZapretPath();
        var bootstrap = new BootstrapWindow(target);
        if (bootstrap.ShowDialog() != true)
            return false;

        if (!ZapretPaths.IsValidZapretRoot(target))
            return false;

        settings.ZapretRoot = target;
        settings.Save();
        return true;
    }
}
