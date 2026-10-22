using Habbo.Domain.Users;

namespace Habbo.Domain.Subscriptions;

/// <summary>One product a player is subscribed to, such as Habbo Club or the Builders Club.
/// A player holds one row per product, and everything the client is told about their
/// membership is worked out from when it runs to.</summary>
public sealed class Subscription
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The client's own name for the product — it branches on "habbo_club" and on
    /// anything starting with "builders_club", and passes the rest through untouched.</summary>
    public required string ProductName { get; set; }

    /// <summary>When the membership runs out. In the past for one that has lapsed, which is
    /// what makes a former member different from someone who never joined.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>How many periods have been paid for beyond the one running now.</summary>
    public int PeriodsSubscribedAhead { get; set; }

    /// <summary>How many periods the player has been a member for in total.</summary>
    public int MemberPeriods { get; set; }

    /// <summary>Days of membership already used up, which the client shows apart from the
    /// current period. Club and VIP are counted separately because the client displays both.
    /// </summary>
    public int PastClubDays { get; set; }

    public int PastVipDays { get; set; }

    /// <summary>When the membership was last changed — bought, extended or cancelled. The
    /// client counts minutes from it to decide how recent a change is.</summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>When the player first subscribed to the product.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>When the unbroken run of membership that is running now began; null while lapsed.
    /// The club's payday pays a bonus for a long one.</summary>
    public DateTimeOffset? StreakStartedAt { get; set; }

    /// <summary>Whether the membership is currently running.</summary>
    public bool IsActive(DateTimeOffset now) => ExpiresAt > now;
}
