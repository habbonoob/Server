namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>The catalog's extras (config "Catalog"): the bonus rare given for spending credits, and the
/// seasonal calendar's page and first day.</summary>
public sealed class CatalogExtrasOptions
{
    public string BonusRareClassName { get; set; } = string.Empty;

    public int BonusRareCredits { get; set; }

    /// <summary>How long after a limited edition goes on sale those who try to buy it are entered in a raffle for it
    /// instead; 0 sells it first come, first served.</summary>
    public int LtdRaffleSeconds { get; set; }

    public string SeasonalCalendarPage { get; set; } = string.Empty;

    public DateOnly SeasonalCalendarStart { get; set; }
}
