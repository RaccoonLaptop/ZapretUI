namespace ZapretUI.Controls.Backgrounds;

public static class BackgroundMotion
{
    /// <summary>15% на ползунке = 1.0 (как в v1.2.6 при первой установке).</summary>
    public static double SpeedFromPercent(int percent)
    {
        percent = Math.Clamp(percent, 5, 100);
        return percent / 15.0;
    }

    public static int PercentFromLegacyMultiplier(double speed)
    {
        speed = Math.Clamp(speed, 0.03, 6.0);
        return (int)Math.Round(Math.Clamp(speed * 15.0, 5, 100));
    }
}
