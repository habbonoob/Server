using Habbo.Silo.Users;
using Habbo.Domain.Friends;
using Habbo.Domain.Subscriptions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Friends;

/// <summary>Reads and changes a player's friend list. Friendship is stored a row per direction,
/// so a player's friends are the rows they own and nothing has to be read twice or normalised on
/// the way out. Messages between friends belong to the console, not here.</summary>
public sealed class FriendListService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    IOptions<FriendListOptions> options) : IFriendListService
{
    /// <summary>Who asked whom lately: the same person is asked at most once in this long, turned down or not.</summary>
    private readonly RecentlySeen<(Guid Requester, Guid Target)> _asked = new(TimeSpan.FromMinutes(10));

    public async Task<FriendListView> GetFriendListAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var categories = await db.FriendCategories
            .AsNoTracking()
            .Where(category => category.UserId == playerId)
            .OrderBy(category => category.Name)
            .Select(category => new FriendCategoryView(category.Id, category.Name))
            .ToListAsync(ct);

        var rows = await db.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.UserId == playerId)
            .Select(friendship => new
            {
                friendship.FriendId,
                friendship.Friend!.AccountId,
                friendship.Friend.Username,
                friendship.Friend.Gender,
                friendship.Friend.Figure,
                friendship.Friend.Motto,
                friendship.Friend.RealName,
                friendship.Friend.ClubLevel,
                friendship.Friend.FollowingBlocked,
                friendship.CategoryId,
                friendship.Relationship,
            })
            .ToListAsync(ct);

        // Who is online is live rather than stored, so the whole list is asked about at once; a friend who hides
        // being online from the player shows offline.
        var friendIds = rows.Select(row => row.FriendId).ToList();
        var online = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync(friendIds))
            .ToHashSet();
        online.ExceptWith(await OnlineVisibility.HiddenFromAsync(db, playerId, friendIds, ct));

        // A friend can be followed when they are in a room and let their friends follow them.
        var inRoom = new HashSet<Guid>();
        foreach (var row in rows.Where(row => online.Contains(row.FriendId) && !row.FollowingBlocked))
        {
            if (await grains.GetGrain<IPlayerGrain>(row.FriendId).GetCurrentRoomAsync() is not null)
                inRoom.Add(row.FriendId);
        }

        var friends = rows
            .Select(row => new FriendView(
                row.AccountId,
                row.Username,
                row.Gender,
                online.Contains(row.FriendId),
                row.Figure,
                row.CategoryId ?? NoCategory,
                row.Motto,
                row.RealName,
                row.Relationship,
                row.ClubLevel >= ClubLevel.Club,
                FollowingAllowed: inRoom.Contains(row.FriendId)))
            .OrderByDescending(friend => friend.Online)
            .ThenBy(friend => friend.Username)
            .ToList();

        // each of the player's groups is a chat in the console
        friends.AddRange(await GroupChatsAsync(db, playerId, ct));

        var limits = options.Value;
        return new FriendListView(
            await FriendLimitAsync(db, playerId, ct),
            limits.NormalFriendLimit,
            limits.ExtendedFriendLimit,
            categories,
            friends);
    }

    public async Task<IReadOnlyList<FriendRequestView>> GetFriendRequestsAsync(
        Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.FriendRequests
            .AsNoTracking()
            .Where(request => request.TargetId == playerId)
            .OrderBy(request => request.CreatedAt)
            .Select(request => new FriendRequestView(
                request.Requester!.AccountId, request.Requester.Username, request.Requester.Figure))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FriendRemoval>> RemoveFriendsAsync(
        Guid playerId, IReadOnlyList<int> friendAccountIds, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // no more than the largest friend list can hold: the client sends the list, so it is not trusted to be short
        var accountIds = friendAccountIds.Distinct().Take(options.Value.ExtendedFriendLimit).ToList();
        var friends = await db.Users
            .Where(user => accountIds.Contains(user.AccountId))
            .Select(user => new { user.Id, user.AccountId })
            .ToListAsync(ct);
        var friendIds = friends.Select(friend => friend.Id).ToList();

        // Friendship is two rows, so dropping it takes both: leaving the other behind would
        // show the friend somebody who no longer has them.
        var rows = await db.Friendships
            .Where(row => (row.UserId == playerId && friendIds.Contains(row.FriendId))
                          || (row.FriendId == playerId && friendIds.Contains(row.UserId)))
            .ToListAsync(ct);
        if (rows.Count == 0)
            return [];

        db.Friendships.RemoveRange(rows);
        await db.SaveChangesAsync(ct);

        var gone = rows.Select(row => row.UserId == playerId ? row.FriendId : row.UserId).ToHashSet();
        return friends.Where(friend => gone.Contains(friend.Id)).Select(friend => new FriendRemoval(friend.Id, friend.AccountId)).ToList();
    }

    public async Task<FriendRequestResult> RequestFriendAsync(
        Guid playerId, string username, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var target = await db.Users.FirstOrDefaultAsync(
            user => user.Username.ToLower() == username.ToLower(), ct);
        if (target is null)
            return Failed(FriendRequestFailure.UnknownUser);
        if (target.Id == playerId)
            return Failed(FriendRequestFailure.Themselves);

        if (await db.Friendships.AnyAsync(f => f.UserId == playerId && f.FriendId == target.Id, ct))
            return Failed(FriendRequestFailure.AlreadyFriends);

        // Being blocked is not told apart from any other refusal on the wire, but it is kept
        // separate here so the reason is never lost on the way out.
        if (await db.BlockedUsers.AnyAsync(b => b.UserId == target.Id && b.BlockedUserId == playerId, ct))
            return Failed(FriendRequestFailure.Blocked);
        if (await db.BlockedUsers.AnyAsync(b => b.UserId == playerId && b.BlockedUserId == target.Id, ct))
            return Failed(FriendRequestFailure.BlockedByRequester);
        if (target.FriendRequestsBlocked)
            return Failed(FriendRequestFailure.RequestsDisabled);
        // requests still waiting count towards the list: one cannot ask more people than the list could take
        if (await db.Friendships.CountAsync(f => f.UserId == playerId, ct) + await db.FriendRequests.CountAsync(r => r.RequesterId == playerId, ct)
            >= await FriendLimitAsync(db, playerId, ct))
            return Failed(FriendRequestFailure.OwnListFull);
        if (await db.Friendships.CountAsync(f => f.UserId == target.Id, ct) >= await FriendLimitAsync(db, target.Id, ct))
            return Failed(FriendRequestFailure.TargetListFull);

        // a turned-down request leaves no row, so the same person is not asked again for a while all the same
        if (await db.FriendRequests.AnyAsync(
                r => r.RequesterId == playerId && r.TargetId == target.Id, ct)
            || !_asked.FirstInWindow((playerId, target.Id)))
            return Failed(FriendRequestFailure.AlreadyAsked);

        var player = await db.Users.FirstAsync(user => user.Id == playerId, ct);
        db.FriendRequests.Add(new FriendRequest { RequesterId = playerId, TargetId = target.Id });
        await db.SaveChangesAsync(ct);

        return new FriendRequestResult(
            FriendRequestFailure.None,
            target.Id,
            new FriendRequestView(player.AccountId, player.Username, player.Figure));

        static FriendRequestResult Failed(FriendRequestFailure failure) =>
            new(failure, Guid.Empty, null);
    }

    public async Task<FriendAcceptance> AcceptFriendsAsync(
        Guid playerId, IReadOnlyList<int> requesterAccountIds, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var requests = await db.FriendRequests
            .Include(request => request.Requester!)
            .Where(request => request.TargetId == playerId
                              && requesterAccountIds.Contains(request.Requester!.AccountId))
            .ToListAsync(ct);
        if (requests.Count == 0)
            return new FriendAcceptance([], []);

        var player = await db.Users.FirstAsync(user => user.Id == playerId, ct);
        var made = new List<FriendshipMade>();
        var failures = new List<FriendAcceptFailure>();
        var ownCount = await db.Friendships.CountAsync(f => f.UserId == playerId, ct);
        var ownLimit = await FriendLimitAsync(db, playerId, ct);

        foreach (var request in requests)
        {
            var requester = request.Requester!;

            // the two asked each other and one already accepted: this request has nothing left to make
            if (await db.Friendships.AnyAsync(f => f.UserId == playerId && f.FriendId == requester.Id, ct))
            {
                db.FriendRequests.Remove(request);
                continue;
            }

            if (ownCount >= ownLimit)
            {
                failures.Add(new FriendAcceptFailure(requester.AccountId, FriendRequestFailure.OwnListFull));
                continue;
            }

            if (await db.Friendships.CountAsync(f => f.UserId == requester.Id, ct) >= await FriendLimitAsync(db, requester.Id, ct))
            {
                failures.Add(new FriendAcceptFailure(requester.AccountId, FriendRequestFailure.TargetListFull));
                continue;
            }

            // A row each way: the two sides file and mark the friendship independently.
            db.Friendships.Add(new Friendship { UserId = playerId, FriendId = requester.Id });
            db.Friendships.Add(new Friendship { UserId = requester.Id, FriendId = playerId });
            db.FriendRequests.Remove(request);
            // and the player's own request the other way, if they had made one, is answered by this too
            await db.FriendRequests.Where(r => r.RequesterId == playerId && r.TargetId == requester.Id).ExecuteDeleteAsync(ct);
            ownCount++;
            // A friendship just made is announced to both sides, who are both there to hear it.
            made.Add(new FriendshipMade(
                requester.Id, ToFriendView(requester, online: true), ToFriendView(player, online: true)));
        }

        await db.SaveChangesAsync(ct);

        // both sides of each friendship count their friends again
        foreach (var userId in made.Select(m => m.RequesterId).Append(playerId).Distinct().ToList())
        {
            var friends = await db.Friendships.CountAsync(f => f.UserId == userId, ct);
            await grains.GetGrain<IPlayerGrain>(userId).ReachAchievementAsync(Achievements.AchievementNames.FriendListSize, friends);
        }

        return new FriendAcceptance(made, failures);
    }

    public async Task DeclineFriendsAsync(
        Guid playerId, bool declineAll, IReadOnlyList<int> requesterAccountIds, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var requests = db.FriendRequests.Where(request => request.TargetId == playerId);
        if (!declineAll)
            requests = requests.Where(request => requesterAccountIds.Contains(request.Requester!.AccountId));

        db.FriendRequests.RemoveRange(await requests.ToListAsync(ct));
        await db.SaveChangesAsync(ct);
    }

    public async Task AnnouncePresenceAsync(Guid playerId, bool online, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Whoever holds a row pointing at the player is whose list shows them, under a category and with a
        // relationship of their own choosing.
        var rows = await db.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.FriendId == playerId)
            .Select(friendship => new { friendship.UserId, friendship.CategoryId, friendship.Relationship })
            .ToListAsync(ct);
        var listening = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key)
                .WhichAreOnlineAsync(rows.Select(row => row.UserId).ToList()))
            .ToHashSet();
        rows.RemoveAll(row => !listening.Contains(row.UserId));
        if (rows.Count == 0)
            return;

        var player = await db.Users.AsNoTracking().FirstAsync(user => user.Id == playerId, ct);
        var hiddenFrom = online ? await OnlineVisibility.HidesFromAsync(db, playerId, listening, ct) : [];
        // as in the friend list: they can be followed when they are in a room and let their friends follow them
        var followable = online && !player.FollowingBlocked
                         && await grains.GetGrain<IPlayerGrain>(playerId).GetCurrentRoomAsync() is not null;

        // one message for each way the player looks, rather than one per friend
        foreach (var looks in rows.GroupBy(row => (
                     CategoryId: row.CategoryId ?? NoCategory, row.Relationship, Shown: online && !hiddenFrom.Contains(row.UserId))))
        {
            var view = ToFriendView(player, looks.Key.Shown) with
            {
                CategoryId = looks.Key.CategoryId,
                Relationship = looks.Key.Relationship,
                FollowingAllowed = looks.Key.Shown && followable,
            };
            await messenger.SendAsync(looks.Select(row => row.UserId),
                new FriendListUpdateComposer([], [FriendListChangeData.Update(FriendData.From(view))]));
        }
    }

    /// <summary>The error codes the client shows a reason for.</summary>
    private static class MessengerErrors
    {
        public const int NotFriend = 3;
        public const int Blocked = 4;
    }

    private static FriendView ToFriendView(User user, bool online) => new(
        user.AccountId,
        user.Username,
        user.Gender,
        online,
        user.Figure,
        NoCategory,
        user.Motto,
        user.RealName,
        RelationshipStatus.None,
        user.ClubLevel >= ClubLevel.Club);

    /// <summary>The player's groups, as the console lists their chats: under the group's id
    /// negated, with its badge for a face.</summary>
    public static Task<List<FriendView>> GroupChatsAsync(AppDbContext db, Guid playerId, CancellationToken ct = default) =>
        db.GroupMembers.AsNoTracking()
            .Where(member => member.UserId == playerId)
            .OrderBy(member => member.Group!.Name)
            .Select(member => new FriendView(
                -member.GroupId,
                member.Group!.Name,
                Gender.Male,
                true,
                member.Group.BadgeCode,
                NoCategory,
                string.Empty,
                string.Empty,
                RelationshipStatus.None,
                false,
                false,
                true))
            .ToListAsync(ct);

    /// <summary>The client's id for "in no group at all".</summary>
    public const int NoCategory = 0;

    /// <summary>How many friends this player may keep. Club membership is what buys the larger
    /// allowance, so the limit is read from the membership rather than stored on the account.
    /// </summary>
    private async Task<int> FriendLimitAsync(AppDbContext db, Guid playerId, CancellationToken ct)
    {
        var hasClub = await db.Subscriptions.AnyAsync(
            subscription => subscription.UserId == playerId
                            && subscription.ProductName == SubscriptionProducts.HabboClub
                            && subscription.ExpiresAt > DateTimeOffset.UtcNow,
            ct);

        return hasClub ? options.Value.ExtendedFriendLimit : options.Value.NormalFriendLimit;
    }
}
