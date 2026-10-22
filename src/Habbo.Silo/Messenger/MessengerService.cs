using System.Collections.Concurrent;
using Habbo.Silo.Abstractions.Permissions;
using Habbo.Silo.Users;
using Habbo.Domain.Friends;
using Habbo.Domain.Groups;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Friends;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Chat;
using Habbo.Silo.Friends;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Messenger;

/// <summary>The console: messages kept until they are read and for scrolling back, group chats,
/// following friends, inviting them and finding people.</summary>
public sealed class MessengerService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    IFriendListService friendList,
    WordFilter wordFilter,
    IOptions<MessengerOptions> options, PermissionService permissions) : IMessengerService
{
    /// <summary>What the client reads as "this is a message arriving" rather than a receipt.</summary>
    private const int NotAConfirmation = 0;

    /// <summary>An error that carries no text of its own.</summary>
    private const string NoDetail = "";

    /// <summary>What the search result carries in the field the client reads and drops.</summary>
    private const string UnusedSearchField = "";

    /// <summary>How many senders are remembered before those whose wait is over are forgotten.</summary>
    private const int SweepAboveSenders = 1000;

    /// <summary>When each player last sent a console message, for the wait between one and the next.</summary>
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastSent = new();

    /// <summary>Whether the player sent their last message too recently; otherwise this one counts as their last.</summary>
    private bool IsFlooding(Guid playerId)
    {
        var now = DateTimeOffset.UtcNow;
        var interval = TimeSpan.FromMilliseconds(options.Value.MessageIntervalMilliseconds);
        if (_lastSent.TryGetValue(playerId, out var last) && now - last < interval)
            return true;

        _lastSent[playerId] = now;
        if (_lastSent.Count > SweepAboveSenders)
        {
            foreach (var over in _lastSent.Where(s => now - s.Value >= interval).Select(s => s.Key).ToList())
                _lastSent.TryRemove(over, out _);
        }

        return false;
    }

    public async Task SendMessageAsync(Guid playerId, int chatId, string text, int confirmationId)
    {
        text = text.Trim();
        // one message at a time: a group chat reaches every member and is kept, so a flood is dropped
        if (text.Length == 0 || IsFlooding(playerId))
            return;
        if (text.Length > ConsoleMessage.TextMaxLength)
            text = text[..ConsoleMessage.TextMaxLength];
        text = (await wordFilter.FilterAsync(text)).Text;

        await using var db = await dbFactory.CreateDbContextAsync();
        var sender = await db.Users.AsNoTracking().FirstAsync(u => u.Id == playerId);
        var key = Guid.NewGuid().ToString("N");

        // a staff mute silences the console as it does room chat
        if (sender.MutedUntil > DateTimeOffset.UtcNow)
        {
            await messenger.SendAsync(playerId, new InstantMessageErrorComposer(InstantMessageErrors.SenderMuted, chatId, NoDetail));
            return;
        }

        if (chatId < 0)
        {
            var groupId = -chatId;
            var members = await db.GroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId).ToListAsync();
            if (!members.Contains(playerId))
            {
                await messenger.SendAsync(playerId, new InstantMessageErrorComposer(InstantMessageErrors.NotGroupMember, chatId, NoDetail));
                return;
            }

            db.ConsoleMessages.Add(new ConsoleMessage { Key = key, SenderId = playerId, GroupId = groupId, Text = text, Delivered = true });
            await db.SaveChangesAsync();

            var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync(members);
            await messenger.SendAsync(online.Where(id => id != playerId),
                new NewConsoleMessageComposer(chatId, ConsoleMessageType.Text, text, 0, key, NotAConfirmation, sender.AccountId, sender.Username, sender.Figure));
            await messenger.SendAsync(playerId,
                new NewConsoleMessageComposer(chatId, ConsoleMessageType.Text, text, 0, key, confirmationId, sender.AccountId, sender.Username, sender.Figure));
            return;
        }

        var recipient = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == chatId);
        int? error = null;
        if (recipient is null || !await db.Friendships.AnyAsync(f => f.UserId == playerId && f.FriendId == recipient.Id))
            error = InstantMessageErrors.NotFriend;
        else if (await db.BlockedUsers.AnyAsync(b => b.UserId == recipient.Id && b.BlockedUserId == playerId))
            error = InstantMessageErrors.RecipientUnavailable;
        else if (await db.BlockedUsers.AnyAsync(b => b.UserId == playerId && b.BlockedUserId == recipient.Id))
            error = InstantMessageErrors.SenderUnavailable;

        if (error is { } code)
        {
            await messenger.SendAsync(playerId, new InstantMessageErrorComposer(code, chatId, NoDetail));
            return;
        }

        var online1 = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync([recipient!.Id]);
        var delivered = online1.Count > 0;
        db.ConsoleMessages.Add(new ConsoleMessage
        {
            Key = key, SenderId = playerId, RecipientId = recipient.Id, Text = text, Delivered = delivered,
        });
        await db.SaveChangesAsync();

        // A conversation is named after whoever is not reading it, so each side is told its own.
        if (delivered)
        {
            await messenger.SendAsync(recipient.Id,
                new NewConsoleMessageComposer(sender.AccountId, ConsoleMessageType.Text, text, 0, key, NotAConfirmation, sender.AccountId, sender.Username, sender.Figure));
        }

        await messenger.SendAsync(playerId,
            new NewConsoleMessageComposer(chatId, ConsoleMessageType.Text, text, 0, key, confirmationId, sender.AccountId, sender.Username, sender.Figure));
    }

    public async Task DeliverPendingAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pending = await db.ConsoleMessages.Include(m => m.Sender)
            .Where(m => m.RecipientId == playerId && !m.Delivered)
            .OrderBy(m => m.Id)
            .ToListAsync();

        foreach (var message in pending)
        {
            message.Delivered = true;
            await messenger.SendAsync(playerId, new NewConsoleMessageComposer(
                message.Sender!.AccountId, ConsoleMessageType.Text, message.Text, SecondsAgo(message.SentAt), message.Key, NotAConfirmation,
                message.Sender.AccountId, message.Sender.Username, message.Sender.Figure));
        }

        await db.SaveChangesAsync();
    }

    public async Task SendHistoryAsync(Guid playerId, int chatId, string beforeMessageId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var before = string.IsNullOrEmpty(beforeMessageId)
            ? long.MaxValue
            : await db.ConsoleMessages.Where(m => m.Key == beforeMessageId).Select(m => (long?)m.Id).FirstOrDefaultAsync() ?? long.MaxValue;

        IQueryable<ConsoleMessage> conversation;
        if (chatId < 0)
        {
            var groupId = -chatId;
            if (!await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == playerId))
                return;
            conversation = db.ConsoleMessages.Where(m => m.GroupId == groupId);
        }
        else
        {
            var other = await db.Users.Where(u => u.AccountId == chatId).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
            if (other is null)
                return;
            conversation = db.ConsoleMessages.Where(m =>
                (m.SenderId == playerId && m.RecipientId == other) || (m.SenderId == other && m.RecipientId == playerId));
        }

        var page = await conversation.AsNoTracking().Include(m => m.Sender)
            .Where(m => m.Id < before)
            .OrderByDescending(m => m.Id)
            .Take(options.Value.HistoryPageSize)
            .ToListAsync();

        await messenger.SendAsync(playerId, new ConsoleMessageHistoryComposer(chatId, page
            .OrderBy(m => m.Id)
            .Select(m => new MessageHistoryEntryData(
                m.Sender!.AccountId, m.Sender.Username, m.Sender.Figure, ConsoleMessageType.Text, m.Text, SecondsAgo(m.SentAt), m.Key))
            .ToList()));
    }

    public async Task FollowAsync(Guid playerId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var friend = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (friend is null || !await db.Friendships.AnyAsync(f => f.UserId == playerId && f.FriendId == friend.Id))
        {
            await messenger.SendAsync(playerId, new FollowFriendFailedComposer(FollowFriendErrors.NotFriend));
            return;
        }

        await GoToRoomOfAsync(db, playerId, friend);
    }

    public async Task VisitAsync(Guid playerId, string userName)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var lower = userName.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == lower);
        // by name rather than from the friend list, but the same rule as following: friends, or staff who follow anyone
        if (user is null
            || (!await db.Friendships.AnyAsync(f => f.UserId == playerId && f.FriendId == user.Id)
                && !await permissions.HasAsync(playerId, PermissionNames.MessengerFollowAnyone)))
        {
            await messenger.SendAsync(playerId, new FollowFriendFailedComposer(FollowFriendErrors.NotFriend));
            return;
        }

        await GoToRoomOfAsync(db, playerId, user);
    }

    /// <summary>Sends the player into the room somebody is in, unless they are away, not in a room,
    /// keep people from following them, or one has blocked the other (staff follow anyone).</summary>
    private async Task GoToRoomOfAsync(AppDbContext db, Guid playerId, User target)
    {
        var isStaff = await permissions.HasAsync(playerId, PermissionNames.MessengerFollowAnyone);
        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync([target.Id]);
        int? error = null;
        int? roomId = null;
        // checked before being online, so a block does not tell whether they are
        if (!isStaff && (await BlockedBetweenAsync(db, playerId, [target.Id])).Count > 0)
            error = FollowFriendErrors.Prevented;
        else if (online.Count == 0 || (!isStaff && (await OnlineVisibility.HiddenFromAsync(db, playerId, [target.Id])).Count > 0))
            error = FollowFriendErrors.Offline;
        else if (target.FollowingBlocked && !isStaff)
            error = FollowFriendErrors.Prevented;
        else if ((roomId = await grains.GetGrain<IPlayerGrain>(target.Id).GetCurrentRoomAsync()) is null)
            error = FollowFriendErrors.HotelView;

        if (error is { } code)
            await messenger.SendAsync(playerId, new FollowFriendFailedComposer(code));
        else
            await messenger.SendAsync(playerId, new RoomForwardMessageComposer(roomId!.Value));
    }

    /// <summary>Of <paramref name="others"/>, those the player blocked or who blocked the player: blocking keeps
    /// the friendship but stops messages, invites and following both ways.</summary>
    private static async Task<HashSet<Guid>> BlockedBetweenAsync(AppDbContext db, Guid playerId, IReadOnlyCollection<Guid> others) =>
        (await db.BlockedUsers.AsNoTracking()
            .Where(b => (b.UserId == playerId && others.Contains(b.BlockedUserId)) || (b.BlockedUserId == playerId && others.Contains(b.UserId)))
            .Select(b => b.UserId == playerId ? b.BlockedUserId : b.UserId)
            .ToListAsync())
        .ToHashSet();

    public async Task FindNewFriendsAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var population = await grains.GetGrain<IRoomPopulationGrain>(0).GetAsync();
        var busy = population.Where(p => p.Value > 0).OrderByDescending(p => p.Value).Select(p => p.Key).ToList();
        var own = await grains.GetGrain<IPlayerGrain>(playerId).GetCurrentRoomAsync();

        // the busiest open room the player is not already in
        var roomId = await db.Rooms.AsNoTracking()
            .Where(r => busy.Contains(r.Id) && r.Id != own && r.State == Habbo.Domain.Rooms.RoomState.Open)
            .Select(r => r.Id)
            .ToListAsync();
        var target = busy.FirstOrDefault(roomId.Contains);

        await messenger.SendAsync(playerId, new FindFriendsProcessResultComposer(target != 0));
        if (target != 0)
            await messenger.SendAsync(playerId, new RoomForwardMessageComposer(target));
    }

    public async Task SearchAsync(Guid playerId, string query)
    {
        query = query.Trim().ToLowerInvariant();
        if (query.Length == 0)
        {
            await messenger.SendAsync(playerId, new HabboSearchResultComposer([], []));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var friendIds = await db.Friendships.Where(f => f.UserId == playerId).Select(f => f.FriendId).ToListAsync();
        var users = await db.Users.AsNoTracking()
            .Where(u => u.Username.ToLower().StartsWith(query))
            .OrderBy(u => u.Username.Length).ThenBy(u => u.Username)
            .Take(options.Value.SearchResultLimit)
            .ToListAsync();
        var online = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key)
            .WhichAreOnlineAsync(users.Select(u => u.Id).ToList())).ToHashSet();
        online.ExceptWith(await OnlineVisibility.HiddenFromAsync(db, playerId, users.Select(u => u.Id).ToList()));

        HabboSearchResultData Result(User user) => new(
            user.AccountId,
            user.Username,
            user.Motto,
            online.Contains(user.Id),
            online.Contains(user.Id) && !user.FollowingBlocked,
            UnusedSearchField,
            user.Gender.ToLetter()[0],
            user.Figure,
            user.RealName);

        await messenger.SendAsync(playerId, new HabboSearchResultComposer(
            users.Where(u => friendIds.Contains(u.Id)).Select(Result).ToList(),
            users.Where(u => !friendIds.Contains(u.Id) && u.Id != playerId).Select(Result).ToList()));
    }

    public async Task InviteAsync(Guid playerId, IReadOnlyList<int> accountIds, string text)
    {
        text = text.Trim();
        if (text.Length > ConsoleMessage.TextMaxLength)
            text = text[..ConsoleMessage.TextMaxLength];
        text = (await wordFilter.FilterAsync(text)).Text;

        await using var db = await dbFactory.CreateDbContextAsync();
        var sender = await db.Users.AsNoTracking().FirstAsync(u => u.Id == playerId);
        if (sender.MutedUntil > DateTimeOffset.UtcNow)
            return;
        var friends = await db.Friendships.AsNoTracking()
            .Where(f => f.UserId == playerId && accountIds.Contains(f.Friend!.AccountId))
            .Select(f => new { f.FriendId, f.Friend!.AccountId })
            .ToListAsync();
        // a friend either side blocked is not invited, and counts as not reached
        var blocked = await BlockedBetweenAsync(db, playerId, friends.Select(f => f.FriendId).ToList());
        friends.RemoveAll(f => blocked.Contains(f.FriendId));
        var online = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key)
            .WhichAreOnlineAsync(friends.Select(f => f.FriendId).ToList())).ToHashSet();

        var invite = new RoomInviteComposer(sender.AccountId, text);
        await messenger.SendAsync(friends.Where(f => online.Contains(f.FriendId)).Select(f => f.FriendId), invite);

        var missed = accountIds.Except(friends.Where(f => online.Contains(f.FriendId)).Select(f => f.AccountId)).ToList();
        if (missed.Count > 0)
            await messenger.SendAsync(playerId, new RoomInviteErrorComposer(RoomInviteErrorComposer.NotDelivered, missed));
    }

    public async Task SetRelationshipAsync(Guid playerId, int accountId, int status)
    {
        if (!Enum.IsDefined(typeof(RelationshipStatus), (short)status))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.UserId == playerId && f.Friend!.AccountId == accountId);
        if (friendship is null)
            return;

        friendship.Relationship = (RelationshipStatus)status;
        await db.SaveChangesAsync();

        var view = (await friendList.GetFriendListAsync(playerId)).Friends.FirstOrDefault(f => f.AccountId == accountId);
        if (view is not null)
            await messenger.SendAsync(playerId, new FriendListUpdateComposer([], [FriendListChangeData.Update(FriendData.From(view))]));
    }

    public async Task RefreshFriendListAsync(Guid playerId)
    {
        var view = await friendList.GetFriendListAsync(playerId);
        await messenger.SendAsync(playerId, new FriendListUpdateComposer(
            view.Categories.Select(c => new FriendCategoryData(c.Id, c.Name)).ToList(),
            view.Friends.Select(f => FriendListChangeData.Update(FriendData.From(f))).ToList()));
    }

    private static int SecondsAgo(DateTimeOffset time) => (int)Math.Max(0, (DateTimeOffset.UtcNow - time).TotalSeconds);
}
