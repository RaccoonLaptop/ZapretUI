using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZapretUI.Helpers;
using ZapretUI.Services;

namespace ZapretUI.Pages;

public partial class HomePage : UserControl
{
    private readonly ZapretPaths _paths;
    private readonly StrategyService _strategy;
    private readonly AppSettings _settings;
    private ComboBox _strategyCombo = null!;
    private Button _toggleBtn = null!;
    private Ellipse _statusIndicator = null!;
    private TextBlock _statusLabel = null!;
    private readonly DispatcherTimer _statusTimer;

    public HomePage(ZapretPaths paths, StrategyService strategy, AppSettings settings)
    {
        _paths = paths;
        _strategy = strategy;
        _settings = settings;
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += (_, _) => RefreshToggleUi();
        BuildUi();
        _statusTimer.Start();
        RefreshToggleUi();
    }

    private void BuildUi()
    {
        var root = new Grid();
        root.Children.Add(CreateAnimatedBackground());

        var center = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 460
        };

        center.Children.Add(CreateAnimatedHeader());

        center.Children.Add(new TextBlock
        {
            Text = "Обход блокировок",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });
        center.Children.Add(new TextBlock
        {
            Text = "Выберите стратегию и нажмите кнопку",
            FontSize = 15,
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 28)
        });

        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        _statusIndicator = new Ellipse
        {
            Width = 14,
            Height = 14,
            Fill = (Brush)Application.Current.FindResource("ErrorBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusLabel = new TextBlock
        {
            Text = "Остановлен",
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusRow.Children.Add(_statusIndicator);
        statusRow.Children.Add(_statusLabel);
        center.Children.Add(statusRow);

        _strategyCombo = new ComboBox
        {
            MinWidth = 400,
            FontSize = 15,
            Margin = new Thickness(0, 0, 0, 24)
        };
        foreach (var s in _paths.GetStrategyFiles())
            _strategyCombo.Items.Add(s);
        SelectDefaultStrategy();
        center.Children.Add(_strategyCombo);

        _toggleBtn = new Button
        {
            Content = "▶   ЗАПУСТИТЬ",
            Style = (Style)Application.Current.FindResource("PrimaryButton"),
            FontSize = 18,
            Padding = new Thickness(56, 16, 56, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 280
        };
        _toggleBtn.Click += async (_, _) => await ToggleAsync();
        center.Children.Add(_toggleBtn);

        root.Children.Add(center);
        Content = root;
    }

    private UIElement CreateAnimatedBackground()
    {
        var canvas = new Canvas
        {
            Width = 980,
            Height = 620,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        var orbA = MakeBackgroundOrb(70, 140, 260, "AccentBrush", 0.09);
        var orbB = MakeBackgroundOrb(620, 80, 220, "SuccessBrush", 0.08);
        var orbC = MakeBackgroundOrb(740, 360, 240, "WarningBrush", 0.07);

        canvas.Children.Add(orbA);
        canvas.Children.Add(orbB);
        canvas.Children.Add(orbC);

        StartBackgroundOrbAnimation(orbA, 0.0, 44, 30, 11000);
        StartBackgroundOrbAnimation(orbB, 1.0, -36, 26, 9000);
        StartBackgroundOrbAnimation(orbC, 2.0, 32, -34, 12000);

        return canvas;
    }

    private static Ellipse MakeBackgroundOrb(double left, double top, double size, string brushKey, double opacity)
    {
        var orb = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = (Brush)Application.Current.FindResource(brushKey),
            Opacity = opacity
        };
        Canvas.SetLeft(orb, left);
        Canvas.SetTop(orb, top);
        return orb;
    }

    private static void StartBackgroundOrbAnimation(UIElement element, double beginSeconds, double moveX, double moveY, int durationMs)
    {
        var xAnim = new DoubleAnimation
        {
            By = moveX,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        var yAnim = new DoubleAnimation
        {
            By = moveY,
            Duration = TimeSpan.FromMilliseconds(durationMs + 1400),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds + 0.2),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        var oAnim = new DoubleAnimation
        {
            From = 0.04,
            To = 0.12,
            Duration = TimeSpan.FromMilliseconds(durationMs - 1000),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds + 0.1)
        };

        element.BeginAnimation(Canvas.LeftProperty, xAnim);
        element.BeginAnimation(Canvas.TopProperty, yAnim);
        element.BeginAnimation(OpacityProperty, oAnim);
    }

    private UIElement CreateAnimatedHeader()
    {
        var root = new Grid
        {
            Width = 360,
            Height = 120,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var canvas = new Canvas { Width = 360, Height = 120 };

        var line1 = new Line
        {
            X1 = 40,
            Y1 = 62,
            X2 = 320,
            Y2 = 62,
            StrokeThickness = 1.5,
            Stroke = (Brush)Application.Current.FindResource("BorderBrush"),
            Opacity = 0.7
        };
        canvas.Children.Add(line1);

        var line2 = new Line
        {
            X1 = 40,
            Y1 = 78,
            X2 = 320,
            Y2 = 78,
            StrokeThickness = 1.2,
            Stroke = (Brush)Application.Current.FindResource("SurfaceLightBrush"),
            Opacity = 0.8
        };
        canvas.Children.Add(line2);

        var dotA = MakeDot(56, 62, "AccentBrush", 9);
        var dotB = MakeDot(182, 62, "SuccessBrush", 8);
        var dotC = MakeDot(304, 62, "WarningBrush", 9);
        canvas.Children.Add(dotA);
        canvas.Children.Add(dotB);
        canvas.Children.Add(dotC);

        StartDotAnimation(dotA, 0.0, 12.0, 1400);
        StartDotAnimation(dotB, 0.2, 14.0, 1700);
        StartDotAnimation(dotC, 0.4, 10.0, 1500);

        root.Children.Add(canvas);
        return root;
    }

    private static Ellipse MakeDot(double x, double y, string brushKey, double size)
    {
        var dot = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = (Brush)Application.Current.FindResource(brushKey)
        };
        Canvas.SetLeft(dot, x - size / 2);
        Canvas.SetTop(dot, y - size / 2);
        return dot;
    }

    private static void StartDotAnimation(UIElement element, double beginSeconds, double rise, int durationMs)
    {
        var move = new DoubleAnimation
        {
            By = -rise,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        element.BeginAnimation(Canvas.TopProperty, move);

        var fade = new DoubleAnimation
        {
            From = 0.45,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(durationMs + 220),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            BeginTime = TimeSpan.FromSeconds(beginSeconds)
        };
        element.BeginAnimation(OpacityProperty, fade);
    }

    private void SelectDefaultStrategy()
    {
        var preferred = new[] { "general.bat", "general (SIMPLE FAKE).bat" };
        foreach (var name in preferred)
        {
            if (_strategyCombo.Items.Contains(name))
            {
                _strategyCombo.SelectedItem = name;
                return;
            }
        }
        if (!string.IsNullOrEmpty(_settings.LastStrategy) && _strategyCombo.Items.Contains(_settings.LastStrategy))
            _strategyCombo.SelectedItem = _settings.LastStrategy;
        else if (_strategyCombo.Items.Count > 0)
            _strategyCombo.SelectedIndex = 0;
    }

    public void RefreshToggleUi()
    {
        var running = _strategy.IsRunning();
        _statusIndicator.Fill = running
            ? (Brush)Application.Current.FindResource("SuccessBrush")
            : (Brush)Application.Current.FindResource("ErrorBrush");
        _statusLabel.Text = running ? "Работает" : "Остановлен";
        if (running)
        {
            var title = _strategy.GetRunningStrategyTitle();
            if (!string.IsNullOrEmpty(title))
                _statusLabel.Text = $"Работает — {title}";
        }

        _toggleBtn.Content = running ? "⏹   ОСТАНОВИТЬ" : "▶   ЗАПУСТИТЬ";
        _strategyCombo.IsEnabled = !running;
    }

    private async Task ToggleAsync()
    {
        if (_strategy.IsRunning())
        {
            await _strategy.StopStrategyAsync();
            ConsoleLog.Instance.Write("Остановлено");
            RefreshToggleUi();
            return;
        }

        if (_strategyCombo.SelectedItem is not string strategy)
        {
            UiHelpers.ShowError("Выберите стратегию");
            return;
        }

        try
        {
            _settings.LastStrategy = strategy;
            _settings.Save();
            ConsoleLog.Instance.Write($"Запуск: {strategy}");
            await _strategy.StartStrategyAsync(strategy);
            ConsoleLog.Instance.Write("Запущено");
        }
        catch (Exception ex)
        {
            ConsoleLog.Instance.Write($"Ошибка: {ex.Message}");
            UiHelpers.ShowError(ex.Message);
        }
        RefreshToggleUi();
    }
}
