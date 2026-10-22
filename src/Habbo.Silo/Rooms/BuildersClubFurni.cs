using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Rooms;

/// <summary>The Builders Club furniture a player has borrowed, which their club limit counts, and the rules for
/// building with it (docs/builders-club.md).</summary>
public static class BuildersClubFurni
{
    public static Task<int> CountAsync(AppDbContext db, Guid playerId, CancellationToken ct = default) =>
        db.UserItems.CountAsync(item => item.OwnerId == playerId && item.BuildersClub, ct);

    /// <summary>The room's owner, or a group's admin in its room.</summary>
    public static bool MayBuild(bool isOwner, int controllerLevel) =>
        isOwner || controllerLevel == RoomControllerLevels.GroupAdmin;

    /// <summary>No membership time left: the free trial, or a membership run out. Building then hides the room, and
    /// its Builders Club furni locks it to visitors.</summary>
    public static bool IsTrial(BuildersClubView club) => club.SecondsLeft <= 0;

    /// <summary>On the trial, only with nobody but staff visiting.</summary>
    public static bool MayBuildWithVisitors(bool trial, int visitors) => !trial || visitors == 0;

    public static bool HasRoomFor(int borrowed, int limit) => borrowed < limit;
}
