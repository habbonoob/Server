namespace Habbo.Silo.Users;

/// <summary>What goes into the earnings window (config section "Earnings"): the daily gift, and how much more a club
/// member gets of the duckets the window pays.</summary>
public sealed class EarningsOptions
{
    /// <summary>The duckets of the daily gift, before a club member's share; 0 gives none.</summary>
    public int DailyGiftDuckets { get; set; } = 5;

    /// <summary>How many times over a club member gets the window's duckets (the daily gift, achievements, level
    /// progression): "HC members get double duckets".</summary>
    public int ClubDucketMultiplier { get; set; } = 2;
}
