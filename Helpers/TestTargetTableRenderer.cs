using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ZapretUI.Models;

namespace ZapretUI.Helpers;

public static class TestTargetTableRenderer
{
    public static void Render(StackPanel host, IReadOnlyList<TestTargetRow> targets)
    {
        host.Children.Clear();
        if (targets.Count == 0)
            return;

        var rows = TestTargetRowFormatter.DedupeByKey(targets);
        var table = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetIsSharedSizeScope(table, true);
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "Name" });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        for (var i = 0; i < rows.Count; i++)
        {
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddRow(table, rows[i], i);
        }

        host.Children.Add(table);
    }

    private static void AddRow(Grid table, TestTargetRow row, int rowIndex)
    {
        var name = new TextBlock
        {
            Text = row.Name,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12.5,
            Foreground = (Brush)Application.Current.FindResource("TextBrush"),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 10, 4)
        };
        Grid.SetRow(name, rowIndex);
        Grid.SetColumn(name, 0);
        table.Children.Add(name);

        var details = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12.5,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 0, 4)
        };

        if (row.PingOnly)
        {
            details.Inlines.Add(new Run(" | " + TestTargetRowFormatter.FormatPingLabel())
            {
                Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
            });
            details.Inlines.Add(new Run(TestTargetRowFormatter.FormatPingValue(row.Ping))
            {
                Foreground = TestTargetRowFormatter.PingBrush(row.Ping),
                FontWeight = FontWeights.SemiBold
            });
        }
        else
        {
            AppendToken(details, row.Http);
            AppendToken(details, row.Tls12);
            AppendToken(details, row.Tls13);
            details.Inlines.Add(new Run(" | " + TestTargetRowFormatter.FormatPingLabel())
            {
                Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
            });
            details.Inlines.Add(new Run(TestTargetRowFormatter.FormatPingValue(row.Ping))
            {
                Foreground = TestTargetRowFormatter.PingBrush(row.Ping),
                FontWeight = FontWeights.SemiBold
            });
        }

        Grid.SetRow(details, rowIndex);
        Grid.SetColumn(details, 1);
        table.Children.Add(details);
    }

    private static void AppendToken(TextBlock block, string rawToken)
    {
        block.Inlines.Add(new Run(" | ")
        {
            Foreground = (Brush)Application.Current.FindResource("TextMutedBrush")
        });
        block.Inlines.Add(new Run(TestTargetRowFormatter.FormatProtocolToken(rawToken))
        {
            Foreground = TestTargetRowFormatter.TokenBrush(rawToken),
            FontWeight = FontWeights.SemiBold
        });
    }
}
