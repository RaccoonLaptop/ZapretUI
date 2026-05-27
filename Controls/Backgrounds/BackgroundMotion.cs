namespace ZapretUI.Controls.Backgrounds;

public static class BackgroundMotion
{
    /// <summary>5% → ~0.12×, 15% → ~1×, 100% → ~5×.</summary>
    public static double SpeedFromPercent(int percent)
    {
        percent = Math.Clamp(percent, 5, 100);
        return 0.12 + (percent - 5) / 95.0 * 4.88;
    }

    public static int PercentFromLegacyMultiplier(double speed)
    {
        speed = Math.Clamp(speed, 0.03, 1.5);
        var t = Math.Sqrt((speed - 0.03) / 1.47);
        return (int)Math.Round(5 + t * 95);
    }
}
