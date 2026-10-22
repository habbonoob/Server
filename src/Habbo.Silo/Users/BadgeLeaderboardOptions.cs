namespace Habbo.Silo.Users;

/// <summary>The badge leaderboards (config section "BadgeLeaderboards").</summary>
public sealed class BadgeLeaderboardOptions
{
    /// <summary>How many players a board ranks; anyone below is unranked. The live hotel's boards hold 500.</summary>
    public int Size { get; set; } = 500;

    /// <summary>The most entries a page may ask for: the client asks for 50.</summary>
    public int PageSizeMax { get; set; } = 50;

    /// <summary>How long the boards stand before they are counted again.</summary>
    public int RefreshSeconds { get; set; } = 300;
}
