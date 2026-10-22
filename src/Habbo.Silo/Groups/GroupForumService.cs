using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Groups;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.GroupForums;
using Habbo.Silo.Abstractions.Groups;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Habbo.Silo.Permissions;
using Habbo.Silo.Users;
using Habbo.Silo.Collections;

namespace Habbo.Silo.Groups;

/// <summary>Group forums: reading, posting and moderating, each within what the forum's settings
/// allow the player.</summary>
public sealed class GroupForumService(
    IDbContextFactory<AppDbContext> dbFactory,
    PlayerMessenger messenger,
    WordFilter wordFilter,
    IOptions<GroupForumOptions> options, PermissionService permissions, IGrainFactory grains) : IGroupForumService
{
    private readonly RecentlySeen<(Guid Player, int Group)> _forumViews = new(TimeSpan.FromHours(1));

    /// <summary>The forum lists, as groupforum.view.forums_list.{n} names them.</summary>
    private const int MostActiveList = 0;
    private const int MostViewedList = 1;
    private const int MyForumsList = 2;

    /// <summary>What a permission error says, as groupforum.view.error.{error} words it; empty is allowed.</summary>
    private const string Allowed = "";
    private const string ForumDisabled = "forum_disabled";
    private const string NotMember = "not_member";
    private const string NotAdmin = "not_admin";
    private const string NotOwner = "not_owner";

    /// <summary>A thread number that starts a new thread instead of replying.</summary>
    private const int NewThread = 0;

    /// <summary>The ids the forum sends where nobody is named.</summary>
    private const int Nobody = 0;

    public async Task SendForumsListAsync(Guid playerId, int listCode, int startIndex, int amount)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var forums = db.Groups.AsNoTracking().Where(g => g.HasForum && !g.ForumDisabled);
        var since = DateTimeOffset.UtcNow.AddDays(-options.Value.ActiveDays);

        IQueryable<Group> ordered = listCode switch
        {
            MostViewedList => forums.OrderByDescending(g => g.ForumViews).ThenBy(g => g.Id),
            MyForumsList => forums.Where(g => db.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == playerId)).OrderBy(g => g.Name),
            _ => forums.OrderByDescending(g => db.ForumMessages.Count(m => m.GroupId == g.Id && m.CreatedAt >= since)).ThenBy(g => g.Id),
        };

        var total = await ordered.CountAsync();
        var page = await ordered.Skip(Math.Max(0, startIndex)).Take(Math.Clamp(amount, 0, options.Value.MaxPageSize)).ToListAsync();
        var data = new List<ForumData>();
        foreach (var group in page)
            data.Add(await ForumDataAsync(db, group, playerId));

        await messenger.SendAsync(playerId, new ForumsListMessageComposer(listCode, total, startIndex, data));
    }

    public async Task SendForumAsync(Guid playerId, int groupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        if (group is null)
            return;

        // a view a player an hour: the stats are asked for on every visit to the forum, and counting each let anyone
        // push a forum up the most-viewed list by asking again and again
        if (_forumViews.FirstInWindow((playerId, groupId)))
        {
            group.ForumViews++;
            await db.SaveChangesAsync();
        }

        var access = await AccessAsync(db, group, playerId);
        await messenger.SendAsync(playerId, new ForumDataMessageComposer(
            await ForumDataAsync(db, group, playerId),
            (int)group.ForumReadPermission,
            (int)group.ForumPostMessagePermission,
            (int)group.ForumPostThreadPermission,
            (int)group.ForumModeratePermission,
            access.Error(group.ForumReadPermission),
            access.Error(group.ForumPostMessagePermission),
            access.Error(group.ForumPostThreadPermission),
            access.Error(group.ForumModeratePermission),
            access.Error(GroupForumPermission.Everyone),
            access.Rank == GroupRank.Owner || access.IsStaff,
            access.IsStaff));
    }

    public async Task UpdateSettingsAsync(Guid playerId, int groupId, int read, int postMessage, int postThread, int moderate)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        if (group is null)
            return;

        var access = await AccessAsync(db, group, playerId);
        if (access.Rank != GroupRank.Owner && !access.IsStaff)
            return;

        var levels = new[] { read, postMessage, postThread, moderate };
        if (levels.Any(level => !Enum.IsDefined(typeof(GroupForumPermission), level)))
            return;

        // each setting changed: its own achievement
        var player = grains.GetGrain<IPlayerGrain>(playerId);
        foreach (var (before, after, achievement) in new[]
                 {
                     (group.ForumReadPermission, (GroupForumPermission)read, AchievementNames.SelfModForumCanReadSeen),
                     (group.ForumPostMessagePermission, (GroupForumPermission)postMessage, AchievementNames.SelfModForumCanPostSeen),
                     (group.ForumPostThreadPermission, (GroupForumPermission)postThread, AchievementNames.SelfModForumCanPostThrdSeen),
                     (group.ForumModeratePermission, (GroupForumPermission)moderate, AchievementNames.SelfModForumCanModerateSeen),
                 })
        {
            if (before != after)
                await player.ReachAchievementAsync(achievement, 1);
        }

        group.ForumReadPermission = (GroupForumPermission)read;
        group.ForumPostMessagePermission = (GroupForumPermission)postMessage;
        group.ForumPostThreadPermission = (GroupForumPermission)postThread;
        group.ForumModeratePermission = (GroupForumPermission)moderate;
        await db.SaveChangesAsync();
        await SendForumAsync(playerId, groupId);
    }

    public async Task SendThreadsAsync(Guid playerId, int groupId, int startIndex, int amount)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        if (group is null)
            return;

        var access = await AccessAsync(db, group, playerId);
        if (!access.May(group.ForumReadPermission))
            return;

        var visible = VisibleThreads(db, groupId, access, group);
        var threads = await visible
            .OrderByDescending(t => t.Sticky)
            .ThenByDescending(t => t.Messages.Max(m => m.CreatedAt))
            .Skip(Math.Max(0, startIndex))
            .Take(Math.Clamp(amount, 0, options.Value.MaxPageSize))
            .Select(t => t.Id)
            .ToListAsync();

        var marker = await MarkerAsync(db, playerId, groupId);
        var data = new List<ThreadData>();
        foreach (var threadId in threads)
            data.Add(await ThreadDataAsync(db, threadId, marker));

        await messenger.SendAsync(playerId, new ForumThreadsMessageComposer(groupId, startIndex, data));
    }

    public async Task SendThreadAsync(Guid playerId, int groupId, int threadId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        if (group is null)
            return;

        var access = await AccessAsync(db, group, playerId);
        if (!access.May(group.ForumReadPermission)
            || !await VisibleThreads(db, groupId, access, group).AnyAsync(t => t.Id == threadId))
            return;

        await messenger.SendAsync(playerId, new UpdateThreadMessageComposer(
            groupId, await ThreadDataAsync(db, threadId, await MarkerAsync(db, playerId, groupId))));
    }

    public async Task SendMessagesAsync(Guid playerId, int groupId, int threadId, int startIndex, int amount)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        if (group is null)
            return;

        var access = await AccessAsync(db, group, playerId);
        if (!access.May(group.ForumReadPermission)
            || !await VisibleThreads(db, groupId, access, group).AnyAsync(t => t.Id == threadId))
            return;

        var messages = await db.ForumMessages.AsNoTracking()
            .Include(m => m.Author).Include(m => m.Admin)
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.Id)
            .ToListAsync();
        var page = messages
            .Select((message, index) => (message, index))
            .Where(x => IsVisible(x.message.State, access, group))
            .Skip(Math.Max(0, startIndex))
            .Take(Math.Clamp(amount, 0, options.Value.MaxPageSize))
            .ToList();

        var data = new List<ForumMessageData>();
        foreach (var (message, index) in page)
            data.Add(await MessageDataAsync(db, message, index));

        await messenger.SendAsync(playerId, new ThreadMessagesMessageComposer(groupId, threadId, startIndex, data));
    }

    public async Task PostAsync(Guid playerId, int groupId, int threadId, string subject, string message)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum && !g.ForumDisabled);
        if (group is null)
            return;

        var access = await AccessAsync(db, group, playerId);
        message = message.Trim();
        if (message.Length < rules.MessageMinLength || message.Length > ForumMessage.TextMaxLength)
            return;
        // a staff mute silences the forums as it does room chat
        if (await db.Users.AnyAsync(u => u.Id == playerId && u.MutedUntil > DateTimeOffset.UtcNow))
            return;

        // the hotel's cooldown between one message and the next
        var last = await db.ForumMessages.Where(m => m.AuthorId == playerId).MaxAsync(m => (DateTimeOffset?)m.CreatedAt);
        if (last is { } previous && DateTimeOffset.UtcNow - previous < TimeSpan.FromSeconds(rules.PostCooldownSeconds))
            return;

        ForumThread thread;
        if (threadId == NewThread)
        {
            subject = subject.Trim();
            if (!access.May(group.ForumPostThreadPermission)
                || subject.Length < rules.SubjectMinLength || subject.Length > ForumThread.SubjectMaxLength)
                return;

            thread = new ForumThread { GroupId = groupId, AuthorId = playerId, Subject = (await wordFilter.FilterAsync(subject)).Text };
            db.ForumThreads.Add(thread);
            await db.SaveChangesAsync();
        }
        else
        {
            if (await db.ForumThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.GroupId == groupId) is not { } existing)
                return;

            thread = existing;
            var moderator = access.May(group.ForumModeratePermission);
            if (!access.May(group.ForumPostMessagePermission) || (thread.Locked && !moderator) || !IsVisible(thread.State, access, group))
                return;
        }

        var row = new ForumMessage { ThreadId = thread.Id, GroupId = groupId, AuthorId = playerId, Text = (await wordFilter.FilterAsync(message)).Text };
        db.ForumMessages.Add(row);
        await db.SaveChangesAsync();

        var marker = await MarkerAsync(db, playerId, groupId);
        if (threadId == NewThread)
        {
            await messenger.SendAsync(playerId, new PostThreadMessageComposer(groupId, await ThreadDataAsync(db, thread.Id, marker)));
            return;
        }

        var index = await db.ForumMessages.CountAsync(m => m.ThreadId == thread.Id && m.Id < row.Id);
        row = await db.ForumMessages.AsNoTracking().Include(m => m.Author).FirstAsync(m => m.Id == row.Id);
        await messenger.SendAsync(playerId, new PostMessageMessageComposer(groupId, thread.Id, await MessageDataAsync(db, row, index)));
    }

    public async Task UpdateThreadAsync(Guid playerId, int groupId, int threadId, bool sticky, bool locked)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        var thread = await db.ForumThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.GroupId == groupId);
        if (group is null || thread is null || !(await AccessAsync(db, group, playerId)).May(group.ForumModeratePermission))
            return;

        thread.Sticky = sticky;
        thread.Locked = locked;
        await db.SaveChangesAsync();
        await messenger.SendAsync(playerId, new UpdateThreadMessageComposer(
            groupId, await ThreadDataAsync(db, threadId, await MarkerAsync(db, playerId, groupId))));
    }

    public async Task ModerateThreadAsync(Guid playerId, int groupId, int threadId, int state)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        var thread = await db.ForumThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.GroupId == groupId);
        if (group is null || thread is null || !MayModerate(await AccessAsync(db, group, playerId), group, thread.State, state))
            return;

        thread.State = (ForumPostState)state;
        thread.AdminId = playerId;
        thread.AdminActedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await messenger.SendAsync(playerId, new UpdateThreadMessageComposer(
            groupId, await ThreadDataAsync(db, threadId, await MarkerAsync(db, playerId, groupId))));
    }

    public async Task ModerateMessageAsync(Guid playerId, int groupId, int threadId, int messageId, int state)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.HasForum);
        var message = await db.ForumMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ThreadId == threadId && m.GroupId == groupId);
        if (group is null || message is null || !MayModerate(await AccessAsync(db, group, playerId), group, message.State, state))
            return;

        message.State = (ForumPostState)state;
        message.AdminId = playerId;
        message.AdminActedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var index = await db.ForumMessages.CountAsync(m => m.ThreadId == threadId && m.Id < messageId);
        message = await db.ForumMessages.AsNoTracking().Include(m => m.Author).Include(m => m.Admin).FirstAsync(m => m.Id == messageId);
        await messenger.SendAsync(playerId, new UpdateMessageMessageComposer(groupId, threadId, await MessageDataAsync(db, message, index)));
    }

    /// <summary>A forum's moderators hide and restore; only staff hide for good, or undo what staff hid.</summary>
    private static bool MayModerate(ForumAccess access, Group group, ForumPostState current, int requested) =>
        Enum.IsDefined(typeof(ForumPostState), (byte)requested) && (ForumPostState)requested != ForumPostState.Default
        && ((ForumPostState)requested == ForumPostState.PermanentlyHiddenByModerator || current == ForumPostState.PermanentlyHiddenByModerator
            ? access.IsStaff
            : access.May(group.ForumModeratePermission));

    public async Task MarkReadAsync(Guid playerId, IReadOnlyList<ForumReadMark> marks)
    {
        // the client sends the list: no more forums than a page lists, each forum's marks applied in turn to its one marker
        var marked = marks.Select(m => m.GroupId).Distinct().Take(options.Value.MaxPageSize).ToList();
        await using var db = await dbFactory.CreateDbContextAsync();
        var markers = await db.ForumReadMarkers.Where(m => m.UserId == playerId && marked.Contains(m.GroupId)).ToDictionaryAsync(m => m.GroupId);
        var unmarked = marked.Except(markers.Keys).ToList();
        var forums = unmarked.Count == 0 ? []
            : (await db.Groups.Where(g => unmarked.Contains(g.Id) && g.HasForum).Select(g => g.Id).ToListAsync()).ToHashSet();
        foreach (var mark in marks.Where(m => marked.Contains(m.GroupId)))
        {
            if (!markers.TryGetValue(mark.GroupId, out var marker))
            {
                if (!forums.Contains(mark.GroupId))
                    continue;
                marker = new ForumReadMarker { UserId = playerId, GroupId = mark.GroupId };
                db.ForumReadMarkers.Add(marker);
                markers[mark.GroupId] = marker;
            }

            marker.LastReadMessageIndex = mark.Reset ? mark.LastReadMessageIndex : Math.Max(marker.LastReadMessageIndex, mark.LastReadMessageIndex);
        }

        await db.SaveChangesAsync();
        await SendUnreadCountAsync(playerId);
    }

    public async Task SendUnreadCountAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var forums = await db.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == playerId && m.Group!.HasForum && !m.Group.ForumDisabled)
            .Select(m => new
            {
                m.GroupId,
                Total = db.ForumMessages.Count(f => f.GroupId == m.GroupId),
                Read = db.ForumReadMarkers.Where(r => r.UserId == playerId && r.GroupId == m.GroupId)
                    .Select(r => (int?)r.LastReadMessageIndex).FirstOrDefault() ?? 0,
            })
            .ToListAsync();

        await messenger.SendAsync(playerId, new UnreadForumsCountMessageComposer(forums.Count(f => f.Total > f.Read)));
    }

    private async Task<ForumData> ForumDataAsync(AppDbContext db, Group group, Guid playerId)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-options.Value.ActiveDays);
        var total = await db.ForumMessages.CountAsync(m => m.GroupId == group.Id);
        var read = await MarkerAsync(db, playerId, group.Id);
        var last = await db.ForumMessages.AsNoTracking().Include(m => m.Author)
            .Where(m => m.GroupId == group.Id)
            .OrderByDescending(m => m.Id)
            .FirstOrDefaultAsync();

        return new ForumData(
            group.Id,
            group.Name,
            group.Description,
            group.BadgeCode,
            await db.ForumThreads.CountAsync(t => t.GroupId == group.Id && t.State != ForumPostState.PermanentlyHiddenByModerator),
            await db.ForumMessages.CountAsync(m => m.GroupId == group.Id && m.CreatedAt >= since),
            total,
            Math.Max(0, total - read),
            last?.Id ?? Nobody,
            last?.Author?.AccountId ?? Nobody,
            last?.Author?.Username ?? string.Empty,
            last is null ? 0 : SecondsAgo(last.CreatedAt));
    }

    private static async Task<ThreadData> ThreadDataAsync(AppDbContext db, int threadId, int readMarker)
    {
        var thread = await db.ForumThreads.AsNoTracking().Include(t => t.Author).Include(t => t.Admin).FirstAsync(t => t.Id == threadId);
        var messages = await db.ForumMessages.AsNoTracking().Include(m => m.Author)
            .Where(m => m.ThreadId == threadId).OrderBy(m => m.Id).ToListAsync();
        var last = messages.LastOrDefault();

        // a forum's messages are read up to a count: whatever lies past it, forum-wide, is unread
        var readUpTo = await db.ForumMessages.Where(m => m.GroupId == thread.GroupId)
            .OrderBy(m => m.Id).Skip(Math.Max(0, readMarker - 1)).Select(m => (int?)m.Id).FirstOrDefaultAsync();
        var unread = readMarker <= 0 ? messages.Count : messages.Count(m => m.Id > (readUpTo ?? int.MaxValue));

        return new ThreadData(
            thread.Id,
            thread.Author?.AccountId ?? Nobody,
            thread.Author?.Username ?? string.Empty,
            thread.Subject,
            thread.Sticky,
            thread.Locked,
            SecondsAgo(thread.CreatedAt),
            messages.Count,
            unread,
            last?.Id ?? Nobody,
            last?.Author?.AccountId ?? Nobody,
            last?.Author?.Username ?? string.Empty,
            last is null ? 0 : SecondsAgo(last.CreatedAt),
            (byte)thread.State,
            thread.Admin?.AccountId ?? Nobody,
            thread.Admin?.Username ?? string.Empty,
            thread.AdminActedAt is { } acted ? SecondsAgo(acted) : 0);
    }

    private static async Task<ForumMessageData> MessageDataAsync(AppDbContext db, ForumMessage message, int index) => new(
        message.Id,
        index,
        message.Author?.AccountId ?? Nobody,
        message.Author?.Username ?? string.Empty,
        message.Author?.Figure ?? string.Empty,
        SecondsAgo(message.CreatedAt),
        message.Text,
        (byte)message.State,
        message.Admin?.AccountId ?? Nobody,
        message.Admin?.Username ?? string.Empty,
        message.AdminActedAt is { } acted ? SecondsAgo(acted) : 0,
        await db.ForumMessages.CountAsync(m => m.GroupId == message.GroupId && m.AuthorId == message.AuthorId));

    private static IQueryable<ForumThread> VisibleThreads(AppDbContext db, int groupId, ForumAccess access, Group group)
    {
        var moderator = access.May(group.ForumModeratePermission);
        return db.ForumThreads.AsNoTracking().Where(t => t.GroupId == groupId
            && (t.State == ForumPostState.Default || t.State == ForumPostState.RestoredByAdmin
                || (t.State == ForumPostState.HiddenByAdmin && moderator)
                || access.IsStaff));
    }

    private static bool IsVisible(ForumPostState state, ForumAccess access, Group group) => state switch
    {
        ForumPostState.HiddenByAdmin => access.May(group.ForumModeratePermission),
        ForumPostState.PermanentlyHiddenByModerator => access.IsStaff,
        _ => true,
    };

    private static async Task<int> MarkerAsync(AppDbContext db, Guid playerId, int groupId) =>
        await db.ForumReadMarkers.Where(m => m.UserId == playerId && m.GroupId == groupId)
            .Select(m => (int?)m.LastReadMessageIndex).FirstOrDefaultAsync() ?? 0;

    private static int SecondsAgo(DateTimeOffset time) => (int)Math.Max(0, (DateTimeOffset.UtcNow - time).TotalSeconds);

    /// <summary>What the context menu of a group's furniture shows: the group, its home room, and
    /// whether the player is in it and may read its forum.</summary>
    public async Task SendFurniContextMenuInfoAsync(Guid playerId, int itemId, int groupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId) is not { } group)
            return;

        var access = await AccessAsync(db, group, playerId);
        await messenger.SendAsync(playerId, new Habbo.Protocol.Messages.Outgoing.Room.Furniture.GuildFurniContextMenuInfoMessageComposer(
            itemId, group.Id, group.Name, group.RoomId, access.Rank is not null,
            group.HasForum && access.May(group.ForumReadPermission)));
    }

    private async Task<ForumAccess> AccessAsync(AppDbContext db, Group group, Guid playerId)
    {
        var rank = await db.GroupMembers.Where(m => m.GroupId == group.Id && m.UserId == playerId)
            .Select(m => (GroupRank?)m.Rank).FirstOrDefaultAsync();
        var isStaff = await permissions.HasAsync(playerId, PermissionNames.GroupForumStaff);
        return new ForumAccess(rank, isStaff, group.ForumDisabled);
    }

    /// <summary>What the player is to the forum: their rank in its group, and whether they are staff.</summary>
    private sealed record ForumAccess(GroupRank? Rank, bool IsStaff, bool Disabled)
    {
        public bool May(GroupForumPermission permission) => Error(permission) == Allowed;

        public string Error(GroupForumPermission permission)
        {
            if (IsStaff)
                return Allowed;
            if (Disabled)
                return ForumDisabled;

            return permission switch
            {
                GroupForumPermission.Everyone => Allowed,
                GroupForumPermission.Members => Rank is not null ? Allowed : NotMember,
                GroupForumPermission.Admins => Rank is GroupRank.Owner or GroupRank.Admin ? Allowed : NotAdmin,
                _ => Rank == GroupRank.Owner ? Allowed : NotOwner,
            };
        }
    }
}
