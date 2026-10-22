namespace Habbo.Domain.Subscriptions;

/// <summary>One club payday that has come round for a player: what they spent in the period
/// before it and what it paid them back, or would have, had they been a member on the day.</summary>
public sealed class ClubPayday
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The day it came round.</summary>
    public DateTimeOffset PaidAt { get; set; }

    /// <summary>Credits spent in the period it closes.</summary>
    public int CreditsSpent { get; set; }

    /// <summary>The share of the spending it pays back.</summary>
    public int SpendReward { get; set; }

    /// <summary>The bonus the member's unbroken streak earned.</summary>
    public int StreakBonus { get; set; }

    /// <summary>Whether they were a member on the day; a missed payday pays nothing.</summary>
    public bool WasMember { get; set; }

    public int Total => SpendReward + StreakBonus;
}
