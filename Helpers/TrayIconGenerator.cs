using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using DColor = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Icon = System.Drawing.Icon;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using Pen = System.Drawing.Pen;
using RectangleF = System.Drawing.RectangleF;
using SolidBrush = System.Drawing.SolidBrush;
using StringAlignment = System.Drawing.StringAlignment;
using StringFormat = System.Drawing.StringFormat;

namespace ZapretUI.Helpers;

public static class TrayIconGenerator
{
    private static readonly DColor RingIdle = DColor.FromArgb(107, 159, 255);
    private static readonly DColor RingActive = DColor.FromArgb(143, 212, 96);
    private static readonly DColor ArcIdle = DColor.FromArgb(80, 120, 72);
    private static readonly DColor ArcActive = DColor.FromArgb(143, 212, 96);

    public static Icon Create(bool active)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(DColor.Transparent);

            var scale = size / 512f;
            var pad = 8f * scale;
            var outer = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

            using (var grad = new LinearGradientBrush(
                       outer,
                       DColor.FromArgb(12, 14, 22),
                       DColor.FromArgb(32, 38, 58),
                       135f))
                g.FillEllipse(grad, outer);

            var ringColor = active ? RingActive : RingIdle;
            var arcColor = active ? ArcActive : ArcIdle;
            var ringWidth = Math.Max(1f, 10f * scale);
            var arcWidth = Math.Max(1f, 9f * scale);

            using (var ring = new Pen(ringColor, ringWidth))
                g.DrawEllipse(ring, 24f * scale, 24f * scale, size - 48f * scale, size - 48f * scale);

            using (var arcPen = new Pen(arcColor, arcWidth))
            {
                arcPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                arcPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                g.DrawArc(arcPen, 72f * scale, 72f * scale, 368f * scale, 368f * scale, 210, 95);
            }

            using var font = new System.Drawing.Font("Segoe UI", 248f * scale, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(DColor.FromArgb(235, 238, 248));
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("Z", font, textBrush, new RectangleF(0, 18f * scale, size, size), format);
        }

        var handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
