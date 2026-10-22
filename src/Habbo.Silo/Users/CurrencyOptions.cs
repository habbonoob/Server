using Habbo.Domain.Users;

namespace Habbo.Silo.Users;

/// <summary>How players come by the activity-point currencies (config section "Currencies"): paid out while they are
/// online, given by staff, and the seasons a seasonal currency lasts.</summary>
public sealed class CurrencyOptions
{
    /// <summary>How often everyone online and awake is paid; 0 pays nothing.</summary>
    public int PayoutIntervalMinutes { get; set; } = 15;

    /// <summary>What each payout gives, by currency type (0 duckets, 5 diamonds, 101 the first seasonal currency).</summary>
    public Dictionary<int, int> Payouts { get; set; } = [];

    /// <summary>What a club member is paid instead, by type; a type not named here pays the same as to anyone.</summary>
    public Dictionary<int, int> ClubPayouts { get; set; } = [];

    /// <summary>How many times over a happy hour (:happyhour) pays out, and for how long when no time is given.</summary>
    public int HappyHourMultiplier { get; set; } = 2;

    public int HappyHourMinutes { get; set; } = 60;

    /// <summary>What everyone is told when a happy hour starts: {multiplier} and {minutes} are filled in.</summary>
    public string HappyHourAnnouncement { get; set; } = "Happy hour! Currency payouts are {multiplier}x for the next {minutes} minutes.";

    /// <summary>The seasons of the seasonal currencies. A currency with seasons is paid only while one runs, and
    /// everyone's balance of it is reset to nothing when one ends.</summary>
    public List<CurrencySeason> Seasons { get; set; } = [];
}
