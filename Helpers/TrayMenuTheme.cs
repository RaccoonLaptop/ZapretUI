using System.Windows.Forms;
using DColor = System.Drawing.Color;
using SolidBrush = System.Drawing.SolidBrush;
using Pen = System.Drawing.Pen;

namespace ZapretUI.Helpers;

internal sealed class TrayMenuTheme : ProfessionalColorTable
{
    public static readonly DColor Bg = DColor.FromArgb(16, 18, 26);
    public static readonly DColor BgHover = DColor.FromArgb(34, 38, 51);
    public static readonly DColor Border = DColor.FromArgb(45, 51, 72);
    public static readonly DColor Text = DColor.FromArgb(232, 235, 244);
    public static readonly DColor TextMuted = DColor.FromArgb(168, 179, 208);
    public static readonly DColor Accent = DColor.FromArgb(107, 159, 255);
    public static readonly DColor Success = DColor.FromArgb(143, 212, 96);
    public static readonly DColor Error = DColor.FromArgb(240, 112, 136);
}

internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    public TrayMenuRenderer() : base(new TrayMenuTheme()) { }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(TrayMenuTheme.Bg), e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(TrayMenuTheme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(TrayMenuTheme.Border);
        e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new System.Drawing.Rectangle(0, 0, e.Item.Width, e.Item.Height);
        var bg = e.Item.Selected && e.Item.Enabled
            ? TrayMenuTheme.BgHover
            : TrayMenuTheme.Bg;
        e.Graphics.FillRectangle(new SolidBrush(bg), bounds);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var bounds = new System.Drawing.Rectangle(0, 0, e.Item.Width, e.Item.Height);
        e.Graphics.FillRectangle(new SolidBrush(TrayMenuTheme.Bg), bounds);

        if (e.Item.Tag is TrayStatusTag tag)
        {
            DrawStatusText(e, tag);
            return;
        }

        var color = TrayMenuTheme.Text;
        if (!e.Item.Enabled)
            color = TrayMenuTheme.TextMuted;
        else if (e.Item.ForeColor != System.Drawing.SystemColors.ControlText
                 && e.Item.ForeColor != System.Drawing.SystemColors.MenuText
                 && e.Item.ForeColor.A > 0)
            color = e.Item.ForeColor;

        DrawItemText(e.Graphics, e.Item, color);
    }

    private static void DrawStatusText(ToolStripItemTextRenderEventArgs e, TrayStatusTag tag)
    {
        var g = e.Graphics;
        var y = (e.Item.Height - e.TextRectangle.Height) / 2;
        using var dotBrush = new SolidBrush(tag.Running ? TrayMenuTheme.Success : TrayMenuTheme.Error);
        g.FillEllipse(dotBrush, 14, y + 4, 8, 8);

        using var textBrush = new SolidBrush(TrayMenuTheme.Text);
        var textRect = new System.Drawing.RectangleF(
            28,
            e.TextRectangle.Y,
            Math.Max(80, e.TextRectangle.Width - 16),
            e.TextRectangle.Height);
        using var format = new System.Drawing.StringFormat
        {
            LineAlignment = System.Drawing.StringAlignment.Center,
            Alignment = System.Drawing.StringAlignment.Near,
            Trimming = System.Drawing.StringTrimming.EllipsisCharacter
        };
        g.DrawString(e.Text, e.TextFont ?? e.Item.Font, textBrush, textRect, format);
    }

    private static void DrawItemText(System.Drawing.Graphics g, ToolStripItem item, DColor color)
    {
        if (string.IsNullOrEmpty(item.Text)) return;

        var rect = new System.Drawing.RectangleF(
            14,
            0,
            Math.Max(80, item.Width - 28),
            item.Height);

        using var brush = new SolidBrush(color);
        using var format = new System.Drawing.StringFormat
        {
            LineAlignment = System.Drawing.StringAlignment.Center,
            Alignment = System.Drawing.StringAlignment.Near,
            Trimming = System.Drawing.StringTrimming.EllipsisCharacter
        };
        g.DrawString(item.Text, item.Font, brush, rect, format);
    }
}

internal sealed class TrayStatusTag
{
    public bool Running { get; init; }
}
