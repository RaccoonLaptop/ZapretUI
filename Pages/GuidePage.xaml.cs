using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ZapretUI.Services;

namespace ZapretUI.Pages;

/// <summary>
/// Краткий гайд по issue #8405 — только справка, конфиги не меняются.
/// </summary>
public partial class GuidePage : UserControl
{
    public GuidePage(ZapretPaths paths)
    {
        var hints = new SystemHintsService(paths).GetHints();
        BuildUi(hints);
    }

    private void BuildUi(IReadOnlyList<SystemHint> hints)
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = (Brush)Application.Current.FindResource("BgBrush")
        };
        var root = new StackPanel();

        root.Children.Add(Title("Гайд и подсказки"));
        root.Children.Add(Subtitle("Zapret UI · Автор: Niko. На основе community-гайда Flowseal. Конфиги не изменяются автоматически."));

        if (hints.Count > 0)
        {
            root.Children.Add(Section("Проверка системы"));
            var warnCard = Card();
            var warnStack = new StackPanel();
            foreach (var h in hints)
            {
                var color = h.Level switch
                {
                    HintLevel.Error => Application.Current.FindResource("ErrorBrush"),
                    HintLevel.Warning => Application.Current.FindResource("WarningBrush"),
                    _ => Application.Current.FindResource("AccentBrush")
                };
                warnStack.Children.Add(new TextBlock
                {
                    Text = h.Title,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)color,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                warnStack.Children.Add(Muted(h.Description));
                warnStack.Children.Add(new Border { Height = 8 });
            }
            warnCard.Child = warnStack;
            root.Children.Add(warnCard);
        }

        root.Children.Add(Section("Порядок настройки"));
        var steps = Card();
        var stepsList = new StackPanel();
        stepsList.Children.Add(Step("1", "Подготовка", "Отключите VPN. Проверьте прокси (inetcpl.cpl). Добавьте папку zapret в исключения антивируса."));
        stepsList.Children.Add(Step("2", "Secure DNS", "В Chrome/Edge/Firefox включите DNS-over-HTTPS (Google Public DNS). В Windows 11 — в параметрах сети."));
        stepsList.Children.Add(Step("3", "Hosts для Discord", "Сервис → «Обновить Hosts File» — скопируйте блок в системный hosts вручную."));
        stepsList.Children.Add(Step("4", "Тест стратегий", "Сервис → «Тестирование» → 1 (HTTP/HTTPS) → 2 (Scan). Для Ростелеком/МГТС часто подходят номера: 1,5,8,11,19."));
        stepsList.Children.Add(Step("5", "Автозапуск", "Сервис → «Удалить службы» → «Установить службу» с рабочей стратегией."));
        stepsList.Children.Add(Step("6", "Игры (при необходимости)", "Game Filter: включить. IPSet: loaded. Списки IP/доменов редактируйте сами в разделе «Списки»."));
        steps.Child = stepsList;
        root.Children.Add(steps);

        root.Children.Add(Section("Важно"));
        var important = Card();
        var imp = new StackPanel();
        imp.Children.Add(Bullet("Не проверяйте обход в Яндекс.Браузере — он подменяет DNS и даёт ложный минус."));
        imp.Children.Add(Bullet("Путь без кириллицы: лучше C:\\zapret, не «Рабочий стол\\Новая папка»."));
        imp.Children.Add(Bullet("Перед сменой стратегии вручную закройте winws.exe (замочек в трее) или остановите службу."));
        imp.Children.Add(Bullet("После правок в lists\\ — перезапустите службу (Удалить → Установить)."));
        important.Child = imp;
        root.Children.Add(important);

        root.Children.Add(Section("Полезные ссылки"));
        var links = Card();
        var linkStack = new StackPanel();
        AddLink(linkStack, "Полный гайд (GitHub #8405)", "https://github.com/Flowseal/zapret-discord-youtube/issues/8405");
        AddLink(linkStack, "DPI Checkers (проверка провайдера)", "https://github.com/hyperion-cs/dpi-checkers");
        AddLink(linkStack, "Проверка DNS", "https://dnscheck.tools/");
        AddLink(linkStack, "Репозиторий Flowseal", "https://github.com/Flowseal/zapret-discord-youtube");
        AddLink(linkStack, "Документация zapret (bol-van)", "https://github.com/bol-van/zapret/blob/master/docs/quick_start_windows.md");
        links.Child = linkStack;
        root.Children.Add(links);

        scroll.Content = root;
        Content = scroll;
    }

    private static TextBlock Title(string t) => new()
    {
        Text = t, FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8)
    };

    private static TextBlock Subtitle(string t) => new()
    {
        Text = t,
        Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 20)
    };

    private static TextBlock Section(string t) => new()
    {
        Text = t, FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 8)
    };

    private static Border Card() => new()
    {
        Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
        BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(16),
        Margin = new Thickness(0, 0, 0, 12)
    };

    private static TextBlock Muted(string t) => new()
    {
        Text = t,
        Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 8)
    };

    private static StackPanel Step(string num, string title, string desc)
    {
        var p = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        p.Children.Add(new TextBlock
        {
            Text = $"{num}. {title}",
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("AccentBrush")
        });
        p.Children.Add(Muted(desc));
        return p;
    }

    private static TextBlock Bullet(string t) => new()
    {
        Text = "• " + t,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 2, 0, 6)
    };

    private static void AddLink(StackPanel parent, string label, string url)
    {
        var link = new TextBlock { Margin = new Thickness(0, 4, 0, 4), Cursor = System.Windows.Input.Cursors.Hand };
        link.Inlines.Add(new Run(label)
        {
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            TextDecorations = TextDecorations.Underline
        });
        link.MouseLeftButtonUp += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* ignore */ }
        };
        parent.Children.Add(link);
    }
}
