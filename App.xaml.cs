using System.Windows;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = AppSettings.Load();
        LocalizationService.Initialize(settings.Language);

        if (UpdateProgressLauncher.TryParseArgs(e.Args, out var progressLog, out var progressVersion))
        {
            var progressWin = new UpdateInstallProgressWindow(progressLog, progressVersion);
            MainWindow = progressWin;
            progressWin.Show();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZapretUI");
                Directory.CreateDirectory(logDir);
                File.WriteAllText(
                    Path.Combine(logDir, "last-error.log"),
                    args.Exception.ToString());
            }
            catch { /* ignore */ }

            MessageBox.Show(
                LocalizationService.F("app.error_details", args.Exception.Message),
                LocalizationService.T("app.error_title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
            Shutdown();
        };

        if (!EnsureZapretInstalled(settings))
        {
            Shutdown();
            return;
        }

        var zapretRoot = ZapretPaths.DetectRoot(settings.ZapretRoot);
        BundledStrategiesService.DeployTo(zapretRoot);

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
            BundledStrategiesService.DeployTo(root);
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
        BundledStrategiesService.DeployTo(target);
        return true;
    }
}
