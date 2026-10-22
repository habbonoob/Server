using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Friends;

/// <summary>Puts what a player did on the friend bar of each friend who is online: a room event they
/// started, an achievement level they reached.</summary>
public sealed class FriendActivityNotifier(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger)
{
    public Task RoomEventStartedAsync(Guid playerId, string eventName) =>
        NotifyAsync(playerId, FriendNotificationComposer.RoomEvent, eventName);

    public Task AchievementReachedAsync(Guid playerId, string badgeCode) =>
        NotifyAsync(playerId, FriendNotificationComposer.Achievement, badgeCode);

    private async Task NotifyAsync(Guid playerId, int typeCode, string message)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // whoever holds a row pointing at the player has them on their friend bar
        var friendIds = await db.Friendships.AsNoTracking().Where(f => f.FriendId == playerId).Select(f => f.UserId).ToListAsync();
        if (friendIds.Count == 0)
            return;

        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync(friendIds);
        if (online.Count == 0)
            return;

        var accountId = await db.Users.Where(u => u.Id == playerId).Select(u => u.AccountId).FirstAsync();
        await messenger.SendAsync(online, new FriendNotificationComposer(accountId.ToString(), typeCode, message));
    }
}
