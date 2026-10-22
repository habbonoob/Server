namespace Habbo.Silo.Subscriptions;

/// <summary>The club's payday (config section "Club").</summary>
public sealed class ClubOptions
{
    /// <summary>Days between paydays, counted from when the player first joined.</summary>
    public int PaydayIntervalDays { get; set; } = 31;

    /// <summary>The share of the credits spent in a period that payday pays back to a member.</summary>
    public double KickbackPercentage { get; set; } = 0.1;

    /// <summary>The bonus an unbroken membership earns on payday, by how long it has run.</summary>
    public List<ClubStreakBonus> StreakBonuses { get; set; } = [];

    /// <summary>How many days a month of membership is.</summary>
    public int DaysPerMonth { get; set; } = 31;

    /// <summary>How close to its end a membership is offered its discounted extension, in days.</summary>
    public int ExtendOfferDays { get; set; } = 5;

    public int ExtendDiscountPercent { get; set; }

    /// <summary>How many days of membership earn one of the monthly gifts.</summary>
    public int GiftIntervalDays { get; set; } = 31;

    /// <summary>The catalog page whose offers are the monthly gifts.</summary>
    public string GiftsPageName { get; set; } = string.Empty;

    /// <summary>The hotel's page for buying the club by text message, and its market; none, and it cannot be.</summary>
    public string DirectBuyPricePointUrl { get; set; } = string.Empty;

    public string DirectBuyMarket { get; set; } = string.Empty;
}
