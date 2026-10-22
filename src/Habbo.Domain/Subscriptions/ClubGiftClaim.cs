using Habbo.Domain.Users;

namespace Habbo.Domain.Subscriptions;

/// <summary>One of the club's monthly gifts a member took.</summary>
public sealed class ClubGiftClaim
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The gift's catalog offer.</summary>
    public int OfferId { get; set; }

    public DateTimeOffset ClaimedAt { get; set; } = DateTimeOffset.UtcNow;
}
