namespace ZapretUI.Controls.Backgrounds;

/// <summary>
/// Фоны главной страницы (вдохновлено bedolaga-cabinet backgrounds).
/// </summary>
public static class HomeBackgroundCatalog
{
    public sealed record Entry(string Id, string Label);

    public static readonly Entry[] All =
    [
        new("wavy", "Волнистый"),
        new("shooting-stars", "Падающие звёзды"),
        new("meteors", "Метеоры"),
        new("sparkles", "Искры"),
        new("aurora", "Северное сияние"),
        new("vortex", "Вихрь"),
        new("grid", "Сетка"),
        new("dots", "Точки"),
        new("ripple", "Волны (ripple)"),
        new("gradient", "Градиент"),
        new("lines", "Линии"),
        new("spotlight", "Прожектор"),
        new("beams", "Лучи"),
        new("none", "Без анимации")
    ];

    public static string Normalize(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return All[0].Id;
        return All.Any(e => e.Id == id) ? id : All[0].Id;
    }

    public static Entry Get(string? id)
    {
        var normalized = Normalize(id);
        return All.First(e => e.Id == normalized);
    }

    public static Entry Next(string? id)
    {
        var normalized = Normalize(id);
        var index = Array.FindIndex(All, e => e.Id == normalized);
        return All[(index + 1) % All.Length];
    }

    /// <summary>Фоны без движения — ползунок скорости на них не действует.</summary>
    public static bool SupportsMotionSpeed(string? id)
    {
        var normalized = Normalize(id);
        return normalized is not ("none" or "grid" or "dots");
    }
}
