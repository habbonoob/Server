using Habbo.Domain.Friends;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Users;

/// <summary>Who may see whom online (the online indicator preference): a player who lets nobody see shows offline to
/// everyone else, one who lets only relationships see shows offline to the friends they have given no relationship
/// status. Everyone sees themselves. Moderation sees through it; only what players are shown of each other goes here.</summary>
public static class OnlineVisibility
{
    /// <summary>Whether a player with this preference hides being online from a viewer: never from themselves, and
    /// with only relationships allowed, from a viewer they have given no relationship status.</summary>
    public static bool Hides(OnlineIndicatorPreference preference, bool viewerIsSelf, bool viewerHasRelationship) =>
        !viewerIsSelf && preference switch
        {
            OnlineIndicatorPreference.Nobody => true,
            OnlineIndicatorPreference.RelationshipStatus => !viewerHasRelationship,
            _ => false,
        };

    /// <summary>Of <paramref name="playerIds"/>, those who hide being online from <paramref name="viewerId"/>.</summary>
    public static async Task<HashSet<Guid>> HiddenFromAsync(
        AppDbContext db, Guid viewerId, IReadOnlyCollection<Guid> playerIds, CancellationToken ct = default)
    {
        var hiding = await db.UserPreferences.AsNoTracking()
            .Where(p => playerIds.Contains(p.UserId) && p.OnlineIndicator != OnlineIndicatorPreference.Everyone)
            .Select(p => new { p.UserId, p.OnlineIndicator })
            .ToListAsync(ct);
        if (hiding.Count == 0)
            return [];

        var hidingIds = hiding.Select(p => p.UserId).ToList();
        var related = (await db.Friendships.AsNoTracking()
                .Where(f => hidingIds.Contains(f.UserId) && f.FriendId == viewerId && f.Relationship != RelationshipStatus.None)
                .Select(f => f.UserId)
                .ToListAsync(ct))
            .ToHashSet();

        return hiding.Where(p => Hides(p.OnlineIndicator, p.UserId == viewerId, related.Contains(p.UserId)))
            .Select(p => p.UserId)
            .ToHashSet();
    }

    /// <summary>Of <paramref name="viewerIds"/>, those <paramref name="playerId"/> hides being online from.</summary>
    public static async Task<HashSet<Guid>> HidesFromAsync(
        AppDbContext db, Guid playerId, IReadOnlyCollection<Guid> viewerIds, CancellationToken ct = default)
    {
        var preference = await db.UserPreferences.AsNoTracking()
            .Where(p => p.UserId == playerId)
            .Select(p => (OnlineIndicatorPreference?)p.OnlineIndicator)
            .FirstOrDefaultAsync(ct) ?? OnlineIndicatorPreference.Everyone;
        if (preference == OnlineIndicatorPreference.Everyone)
            return [];

        var related = (await db.Friendships.AsNoTracking()
                .Where(f => f.UserId == playerId && viewerIds.Contains(f.FriendId) && f.Relationship != RelationshipStatus.None)
                .Select(f => f.FriendId)
                .ToListAsync(ct))
            .ToHashSet();

        return viewerIds.Where(v => Hides(preference, v == playerId, related.Contains(v))).ToHashSet();
    }
}
