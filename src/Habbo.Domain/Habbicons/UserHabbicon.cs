using Habbo.Domain.Users;

namespace Habbo.Domain.Habbicons;

/// <summary>A Habbicon a player owns: bought, given as a collection's reward, or won (a reward track prize). The
/// Habbicons themselves are seed data (seed/habbicons.json), named here by id.</summary>
public sealed class UserHabbicon
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int HabbiconId { get; set; }

    /// <summary>Starred in the Habbicons Collection, so it shows under the favourites tab.</summary>
    public bool Favourite { get; set; }

    public DateTimeOffset AcquiredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When it was last shown over the avatar in a room; the picker lists the most recent first.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
