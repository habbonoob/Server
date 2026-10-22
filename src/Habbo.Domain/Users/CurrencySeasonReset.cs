namespace Habbo.Domain.Users;

/// <summary>A seasonal currency's balances were reset at the end of a season: recorded so the season is reset once.</summary>
public sealed class CurrencySeasonReset
{
    public int Type { get; set; }

    /// <summary>When the season ended; with the type, which season this was.</summary>
    public DateTimeOffset SeasonEndedAt { get; set; }

    public DateTimeOffset ResetAt { get; set; } = DateTimeOffset.UtcNow;
}
