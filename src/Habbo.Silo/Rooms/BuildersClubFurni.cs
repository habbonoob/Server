using Habbo.Domain.Rooms;
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

    /// <summary>How many Builders Club items one on the free trial may have out: 50, as habbo.com's trial gives. (One
    /// capture of an account that never joined, 25 October 2026, was told 1000; the hotel's owner knows the trial as 50.)
    /// </summary>
    public const int TrialFurniLimit = 50;

    /// <summary>On the trial, only with nobody but staff visiting.</summary>
    public static bool MayBuildWithVisitors(bool trial, int visitors) => !trial || visitors == 0;

    public static bool HasRoomFor(int borrowed, int limit) => borrowed < limit;

    /// <summary>A room locked for the trial unlocks when the last Builders Club furni has left it.</summary>
    public static bool UnlocksWithoutFurni(bool locked, bool holdsBorrowedFurni) => locked && !holdsBorrowedFurni;

    /// <summary>The door a room goes back to when unlocked: the one it had. A room locked before that was kept has lost it to
    /// the lock's invisible door, and opens: left invisible, it would still look locked. A door staff have set since stays.
    /// </summary>
    public static RoomState DoorWhenUnlocked(RoomState? remembered, RoomState current) =>
        remembered ?? (current == RoomState.Invisible ? RoomState.Open : current);
}
