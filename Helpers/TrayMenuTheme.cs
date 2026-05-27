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

    public override DColor MenuItemSelected => BgHover;
    public override DColor MenuItemSelectedGradientBegin => BgHover;
    public override DColor MenuItemSelectedGradientEnd => BgHover;
    public override DColor MenuItemPressedGradientBegin => BgHover;
    public override DColor MenuItemPressedGradientEnd => BgHover;
    public override DColor MenuItemBorder => Border;
    public override DColor MenuBorder => Border;
    public override DColor ToolStripDropDownBackground => Bg;
    public override DColor ImageMarginGradientBegin => Bg;
    public override DColor ImageMarginGradientMiddle => Bg;
    public override DColor ImageMarginGradientEnd => Bg;
    public override DColor SeparatorDark => Border;
    public override DColor SeparatorLight => Border;
}

internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    public TrayMenuRenderer() : base(new TrayMenuTheme()) { }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(TrayMenuTheme.Bg), e.AffectedBounds);
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
        var g = e.Graphics;
        var bounds = new System.Drawing.Rectangle(0, 0, e.Item.Width, e.Item.Height);
        if (e.Item is ToolStripLabel)
        {
            g.FillRectangle(new SolidBrush(TrayMenuTheme.Bg), bounds);
            return;
        }

        if (!e.Item.Enabled)
        {
            g.FillRectangle(new SolidBrush(TrayMenuTheme.Bg), bounds);
            return;
        }

        if (e.Item.Selected)
            g.FillRectangle(new SolidBrush(TrayMenuTheme.BgHover), bounds);
        else
            g.FillRectangle(new SolidBrush(TrayMenuTheme.Bg), bounds);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item is ToolStripLabel && e.Item.Tag is TrayStatusTag tag)
        {
            var g = e.Graphics;
            var y = (e.Item.Height - e.TextRectangle.Height) / 2;
            using var dotBrush = new SolidBrush(tag.Running ? TrayMenuTheme.Success : TrayMenuTheme.Error);
            g.FillEllipse(dotBrush, 14, y + 4, 8, 8);

            using var textBrush = new SolidBrush(TrayMenuTheme.Text);
            var textRect = new System.Drawing.RectangleF(28, e.TextRectangle.Y, e.TextRectangle.Width - 16, e.TextRectangle.Height);
            using var format = new System.Drawing.StringFormat
            {
                LineAlignment = System.Drawing.StringAlignment.Center,
                Alignment = System.Drawing.StringAlignment.Near,
                Trimming = System.Drawing.StringTrimming.EllipsisCharacter
            };
            g.DrawString(e.Text, e.TextFont ?? e.Item.Font, textBrush, textRect, format);
            return;
        }

        e.TextColor = e.Item.Enabled ? TrayMenuTheme.Text : TrayMenuTheme.TextMuted;
        base.OnRenderItemText(e);
    }
}

internal sealed class TrayStatusTag
{
    public bool Running { get; init; }
}
