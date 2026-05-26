using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI;

public class SetupWindow : Window
{
    private readonly SecuritySetupService _service;
    private readonly AppSettings _settings;
    private CheckBox _defenderCheck = null!;
    private CheckBox _firewallCheck = null!;
    private TextBlock _statusText = null!;

    public SetupWindow(SecuritySetupService service, AppSettings settings)
    {
        _service = service;
        _settings = settings;
        BuildUi();
    }

    private void BuildUi()
    {
        Title = "Установка Zapret UI — Niko";
        Width = 520;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = (Brush)Application.Current.FindResource("BgBrush");

        var root = new StackPanel { Margin = new Thickness(24) };

        root.Children.Add(new TextBlock
        {
            Text = "Настройка безопасности",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        root.Children.Add(new TextBlock
        {
            Text = "Zapret использует WinDivert и winws.exe — антивирус и брандмауэр могут их блокировать. Рекомендуем добавить исключения сейчас (нужны права администратора).",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 20)
        });

        var plan = _service.BuildPlan();
        var pathsBlock = new TextBlock
        {
            Text = "Будут добавлены:\n" + string.Join("\n", plan.ExclusionPaths.Concat(plan.ProgramPaths).Select(p => "• " + p)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 16)
        };
        root.Children.Add(pathsBlock);

        _defenderCheck = new CheckBox
        {
            Content = "Добавить папки в исключения Windows Defender",
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _firewallCheck = new CheckBox
        {
            Content = "Разрешить ZapretUI.exe и winws.exe в брандмауэре Windows",
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 16)
        };
        root.Children.Add(_defenderCheck);
        root.Children.Add(_firewallCheck);

        _statusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            MinHeight = 40,
            Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(_statusText);

        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var skipBtn = new Button
        {
            Content = "Пропустить",
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        skipBtn.Click += (_, _) => CloseSkipped();

        var applyBtn = new Button
        {
            Content = "Применить",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            IsDefault = true,
            MinWidth = 120
        };
        applyBtn.Click += async (_, _) => await ApplyAsync(applyBtn);

        btns.Children.Add(skipBtn);
        btns.Children.Add(applyBtn);
        root.Children.Add(btns);

        Content = root;
    }

    private async Task ApplyAsync(Button applyBtn)
    {
        applyBtn.IsEnabled = false;
        _statusText.Text = "Применение... Подтвердите UAC.";

        var result = await _service.ApplyAsync(
            _defenderCheck.IsChecked == true,
            _firewallCheck.IsChecked == true);

        if (result.Success)
        {
            _settings.SecuritySetupCompleted = true;
            _settings.SecuritySetupSkipped = false;
            _settings.Save();
            ConsoleLog.Instance.Write(result.Message);
            DialogResult = true;
            Close();
            return;
        }
        else
        {
            _statusText.Text = result.Message;
            _statusText.Foreground = (Brush)Application.Current.FindResource("ErrorBrush");
            applyBtn.IsEnabled = true;
        }
    }

    private void CloseSkipped()
    {
        _settings.SecuritySetupSkipped = true;
        _settings.Save();
        DialogResult = false;
        Close();
    }
}
