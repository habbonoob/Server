using Habbo.Silo.Abstractions.Permissions;
using System.Collections.Concurrent;
using System.Globalization;
using Habbo.Domain.Moderation;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.CallForHelp;
using Habbo.Protocol.Messages.Outgoing.Help;
using Habbo.Protocol.Messages.Outgoing.Moderation;
using Habbo.Silo.Abstractions.Moderation;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Permissions;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Moderation;

/// <summary>The mod tool and calls for help: issues coming in and worked through, chat logs and
/// visits, and the sanctions staff give.</summary>
public sealed class ModerationService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    PermissionService permissions,
    IOptions<ModerationOptions> options) : IModerationService
{
    /// <summary>What a report's pending-call answer and a moderator message carry where there is nothing to add.</summary>
    private const string NoText = "";

    /// <summary>A report accepted: the client thanks the reporter.</summary>
    private const int ReportAccepted = 0;

    /// <summary>The chat log a report's reporter picked lines for, and the room's.</summary>
    private const string RoomIdKey = "roomId";
    private const string RoomNameKey = "roomName";
    private const string GroupIdKey = "groupId";
    private const string ThreadIdKey = "threadId";
    private const string MessageIdKey = "messageId";
    private const string UrlKey = "url";

    /// <summary>The room actions the room tool sends: a caution or a message, with or without a kick.</summary>
    private const int RoomCaution = 0;
    private const int RoomCautionAndKick = 1;
    private const int RoomMessage = 3;
    private const int RoomMessageAndKick = 4;

    /// <summary>What the mod tool's user info says of a user who has never bought anything.</summary>
    private const string Never = "";

    /// <summary>What IssueDeleted and the pending calls name an issue by: its number as text.</summary>
    private static string Key(int issueId) => issueId.ToString(CultureInfo.InvariantCulture);

    public async Task SendInitAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rank = await RankOfAsync(db, playerId);
        bool May(string permission) => permissions.Has(playerId, rank, permission);
        if (!May(PermissionNames.ModerationTool))
            return;

        var issues = await OpenIssuesQuery(db).ToListAsync();
        var presets = await db.ModerationPresets.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync();
        var preference = await db.ModToolPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == playerId);

        await messenger.SendAsync(playerId, new ModeratorInitMessageComposer(
            issues.Select(ToIssueData).ToList(),
            presets.Where(p => p.Kind == ModerationPresetKind.User).Select(p => p.Text).ToList(),
            [],
            May(PermissionNames.ModerationCallForHelp),
            May(PermissionNames.ModerationChatlog),
            May(PermissionNames.ModerationAlert),
            May(PermissionNames.ModerationKick),
            May(PermissionNames.ModerationBan),
            May(PermissionNames.ModerationRoomAlert),
            May(PermissionNames.ModerationRoomKick),
            presets.Where(p => p.Kind == ModerationPresetKind.Room).Select(p => p.Text).ToList()));

        if (preference is not null)
        {
            await messenger.SendAsync(playerId, new ModeratorToolPreferencesComposer(
                preference.WindowX, preference.WindowY, preference.WindowWidth, preference.WindowHeight));
        }
    }

    public async Task RecordMachineAsync(Guid playerId, string machineId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.FirstOrDefaultAsync(u => u.Id == playerId) is { } user && user.MachineId != machineId)
        {
            user.MachineId = machineId.Length > MachineIdMaxLength ? machineId[..MachineIdMaxLength] : machineId;
            await db.SaveChangesAsync();
        }
    }

    private const int MachineIdMaxLength = 128;

    public async Task<ActiveBan?> FindBanAsync(Guid playerId, string machineId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var ban = await db.Sanctions.AsNoTracking()
            .Where(s => s.Kind == SanctionKind.Ban && s.ExpiresAt > now
                        && (s.UserId == playerId || (!s.AvatarOnly && s.MachineId != string.Empty && s.MachineId == machineId)))
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefaultAsync();
        return ban is null ? null : new ActiveBan(ban.Message, ban.ExpiresAt!.Value);
    }

    public async Task PickIssuesAsync(Guid moderatorId, IReadOnlyList<int> issueIds, bool retryEnabled, int retryCount, string message)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationCallForHelp))
            return;

        var issues = await db.ModerationIssues.Include(i => i.Picker)
            .Where(i => issueIds.Contains(i.Id) && i.State != ModerationIssueState.Closed)
            .ToListAsync();
        var taken = issues.Where(i => i.State == ModerationIssueState.Picked && i.PickerId != moderatorId).ToList();
        if (taken.Count > 0)
        {
            await messenger.SendAsync(moderatorId, new IssuePickFailedMessageComposer(
                taken.Select(i => new PickFailedIssueData(i.Id, i.Picker?.AccountId ?? 0, i.Picker?.Username ?? string.Empty)).ToList(),
                retryEnabled, retryCount));
            return;
        }

        foreach (var issue in issues)
        {
            issue.State = ModerationIssueState.Picked;
            issue.PickerId = moderatorId;
        }

        await db.SaveChangesAsync();
        await BroadcastIssuesAsync(db, issues.Select(i => i.Id));
    }

    public async Task ReleaseIssuesAsync(Guid moderatorId, IReadOnlyList<int> issueIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var issues = await db.ModerationIssues
            .Where(i => issueIds.Contains(i.Id) && i.State == ModerationIssueState.Picked && i.PickerId == moderatorId)
            .ToListAsync();
        foreach (var issue in issues)
        {
            issue.State = ModerationIssueState.Open;
            issue.PickerId = null;
        }

        await db.SaveChangesAsync();
        await BroadcastIssuesAsync(db, issues.Select(i => i.Id));
    }

    public async Task CloseIssuesAsync(Guid moderatorId, IReadOnlyList<int> issueIds, int closeReason)
    {
        if (!Enum.IsDefined(typeof(ModerationCloseReason), closeReason))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationCallForHelp))
            return;

        await CloseAsync(db, moderatorId, issueIds, (ModerationCloseReason)closeReason, sanctioned: false);
    }

    public async Task CloseWithDefaultSanctionAsync(Guid moderatorId, int issueId, IReadOnlyList<int> otherIssueIds, int topicId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // only an issue this moderator picked and has not closed: a repeat must not climb the ladder again
        var issue = await db.ModerationIssues.Include(i => i.ReportedUser).FirstOrDefaultAsync(i =>
            i.Id == issueId && i.State == ModerationIssueState.Picked && i.PickerId == moderatorId);
        if (issue is null || !await MayAsync(db, moderatorId, PermissionNames.ModerationCallForHelp))
            return;

        // resolved with a sanction only when the sanction was given: one the moderator may not give leaves the issue open
        if (issue.ReportedUser is { } reported && !await TryDefaultSanctionAsync(moderatorId, reported.AccountId, topicId, NoText, issueId))
            return;

        await CloseAsync(db, moderatorId, [issueId, .. otherIssueIds], ModerationCloseReason.Resolved, sanctioned: issue.ReportedUser is not null);
    }

    /// <summary>Closes issues the moderator picked: the reporters are told how, and the mod tools drop them. Another
    /// moderator's issues, and those nobody picked, are theirs to close.</summary>
    private async Task CloseAsync(AppDbContext db, Guid moderatorId, IReadOnlyList<int> issueIds, ModerationCloseReason reason, bool sanctioned)
    {
        var issues = await db.ModerationIssues
            .Where(i => issueIds.Contains(i.Id) && i.State == ModerationIssueState.Picked && i.PickerId == moderatorId).ToListAsync();
        foreach (var issue in issues)
        {
            issue.State = ModerationIssueState.Closed;
            issue.ClosedAt = DateTimeOffset.UtcNow;
            issue.CloseReason = reason;
            issue.Sanctioned = sanctioned;
        }

        await db.SaveChangesAsync();

        foreach (var issue in issues)
        {
            if (issue.ReporterId is { } reporter)
                await messenger.SendAsync(reporter, new IssueCloseNotificationMessageComposer((int)reason, NoText));
            await messenger.SendAsync(await OnlineModeratorsAsync(db), new IssueDeletedMessageComposer(Key(issue.Id)));
        }
    }

    public async Task SendSanctionPreviewAsync(Guid moderatorId, int issueId, int accountId, int topicId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationCallForHelp))
            return;

        Guid? userId = null;
        if (issueId > 0)
            userId = await db.ModerationIssues.Where(i => i.Id == issueId).Select(i => i.ReportedUserId).FirstOrDefaultAsync();
        else
            userId = await db.Users.Where(u => u.AccountId == accountId).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
        if (userId is null)
            return;

        var next = await NextSanctionTypeAsync(db, userId.Value);
        var account = await db.Users.Where(u => u.Id == userId).Select(u => u.AccountId).FirstAsync();
        await messenger.SendAsync(moderatorId, new CfhSanctionMessageComposer(
            issueId, account, next.Name, next.LengthHours, next.ProbationDays, false, NoText, NoText));
    }

    public async Task SendIssueChatlogAsync(Guid moderatorId, int issueId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationChatlog))
            return;

        var issue = await db.ModerationIssues.AsNoTracking()
            .Include(i => i.Reporter).Include(i => i.ReportedUser).Include(i => i.ChatLines)
            .FirstOrDefaultAsync(i => i.Id == issueId);
        if (issue is null)
            return;

        ChatRecordData record;
        if (issue.GroupId is { } groupId && issue.ThreadId is { } threadId)
        {
            var messages = await db.ForumMessages.AsNoTracking().Include(m => m.Author)
                .Where(m => m.ThreadId == threadId && (issue.ForumMessageId == null || m.Id == issue.ForumMessageId))
                .OrderBy(m => m.Id).Take(options.Value.ChatlogLines).ToListAsync();
            List<ChatRecordContextData> context = [ChatRecordContextData.Of(GroupIdKey, groupId), ChatRecordContextData.Of(ThreadIdKey, threadId)];
            if (issue.ForumMessageId is { } messageId)
                context.Add(ChatRecordContextData.Of(MessageIdKey, messageId));
            record = new ChatRecordData(
                issue.ForumMessageId is null ? ChatRecordData.ForumThread : ChatRecordData.ForumMessage,
                context,
                messages.Select(m => new ChatlineData(Time(m.CreatedAt), m.Author!.AccountId, m.Author.Username, m.Text, false)).ToList());
        }
        else if (issue.PhotoId.Length > 0)
        {
            record = new ChatRecordData(
                issue.Source == ModerationIssueSources.Selfie ? ChatRecordData.Selfie : ChatRecordData.Photo,
                [ChatRecordContextData.Of(UrlKey, issue.PhotoId), ChatRecordContextData.Of(RoomIdKey, issue.RoomId ?? 0)],
                []);
        }
        else if (issue.ChatLines.Count > 0)
        {
            var type = issue.Source == ModerationIssueSources.InstantMessage ? ChatRecordData.InstantMessages : ChatRecordData.Room;
            record = new ChatRecordData(
                type,
                issue.RoomId is { } reportedRoom ? [ChatRecordContextData.Of(RoomIdKey, reportedRoom)] : [],
                issue.ChatLines.Select(l => new ChatlineData(Time(issue.CreatedAt), l.UserAccountId, l.UserName, l.Text,
                    issue.ReportedUser is { } r && l.UserAccountId == r.AccountId)).ToList());
        }
        else if (issue.RoomId is { } roomId)
        {
            record = await RoomRecordAsync(db, roomId, issue.CreatedAt, issue.ReportedUserId);
        }
        else
        {
            record = new ChatRecordData(ChatRecordData.Room, [], []);
        }

        await messenger.SendAsync(moderatorId, new CfhChatlogComposer(
            issue.Id, issue.Reporter?.AccountId ?? 0, issue.ReportedUser?.AccountId ?? 0, issue.Id, record));
    }

    public async Task SendRoomChatlogAsync(Guid moderatorId, int roomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationChatlog))
            return;

        await messenger.SendAsync(moderatorId, new RoomChatlogComposer(await RoomRecordAsync(db, roomId, DateTimeOffset.UtcNow, null)));
    }

    /// <summary>What was said in a room up to a moment, the latest lines last; a user's own
    /// lines highlighted when they are the one it is about.</summary>
    private async Task<ChatRecordData> RoomRecordAsync(AppDbContext db, int roomId, DateTimeOffset upTo, Guid? highlight)
    {
        var name = await db.Rooms.Where(r => r.Id == roomId).Select(r => r.Name).FirstOrDefaultAsync() ?? string.Empty;
        var lines = await db.RoomChatLines.AsNoTracking()
            .Where(l => l.RoomId == roomId && l.SaidAt <= upTo)
            .OrderByDescending(l => l.Id)
            .Take(options.Value.ChatlogLines)
            .Join(db.Users, l => l.UserId, u => u.Id, (l, u) => new { l.Text, l.SaidAt, l.UserId, u.AccountId, u.Username })
            .ToListAsync();

        return new ChatRecordData(
            ChatRecordData.Room,
            [ChatRecordContextData.Of(RoomIdKey, roomId), ChatRecordContextData.Of(RoomNameKey, name)],
            lines.OrderBy(l => l.SaidAt)
                .Select(l => new ChatlineData(Time(l.SaidAt), l.AccountId, l.Username, l.Text, l.UserId == highlight))
                .ToList());
    }

    public async Task SendUserChatlogAsync(Guid moderatorId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (user is null || !await MayAsync(db, moderatorId, PermissionNames.ModerationChatlog))
            return;

        // the rooms they were last in, each with what was said there around their visit
        var visits = await db.RoomVisits.AsNoTracking()
            .Where(v => v.UserId == user.Id)
            .OrderByDescending(v => v.Id)
            .Take(options.Value.UserChatlogRooms)
            .ToListAsync();

        var records = new List<ChatRecordData>();
        foreach (var visit in visits)
        {
            var name = await db.Rooms.Where(r => r.Id == visit.RoomId).Select(r => r.Name).FirstOrDefaultAsync() ?? string.Empty;
            var lines = await db.RoomChatLines.AsNoTracking()
                .Where(l => l.RoomId == visit.RoomId && l.UserId == user.Id && l.SaidAt >= visit.EnteredAt)
                .OrderBy(l => l.Id)
                .Take(options.Value.ChatlogLines)
                .ToListAsync();
            records.Add(new ChatRecordData(
                ChatRecordData.Room,
                [ChatRecordContextData.Of(RoomIdKey, visit.RoomId), ChatRecordContextData.Of(RoomNameKey, name)],
                lines.Select(l => new ChatlineData(Time(l.SaidAt), user.AccountId, user.Username, l.Text, false)).ToList()));
        }

        await messenger.SendAsync(moderatorId, new UserChatlogComposer(user.AccountId, user.Username, records));
    }

    public async Task SendRoomVisitsAsync(Guid moderatorId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (user is null || !await MayAsync(db, moderatorId, PermissionNames.ModerationChatlog))
            return;

        var visits = await db.RoomVisits.AsNoTracking()
            .Where(v => v.UserId == user.Id)
            .OrderByDescending(v => v.Id)
            .Take(options.Value.RoomVisitsLimit)
            .Join(db.Rooms, v => v.RoomId, r => r.Id, (v, r) => new { v.RoomId, r.Name, v.EnteredAt })
            .ToListAsync();

        await messenger.SendAsync(moderatorId, new RoomVisitsComposer(user.AccountId, user.Username, visits
            .Select(v => new RoomVisitData(v.RoomId, v.Name, v.EnteredAt.UtcDateTime.Hour, v.EnteredAt.UtcDateTime.Minute))
            .ToList()));
    }

    public async Task SendRoomInfoAsync(Guid moderatorId, int roomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationTool))
            return;

        var room = await db.Rooms.AsNoTracking().Include(r => r.Owner).FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null)
        {
            await messenger.SendAsync(moderatorId, new ModeratorRoomInfoComposer(roomId, 0, false, 0, string.Empty, false, string.Empty, string.Empty, []));
            return;
        }

        var population = await grains.GetGrain<IRoomPopulationGrain>(0).GetAsync();
        var count = population.GetValueOrDefault(roomId);
        var ownerIn = count > 0 && await grains.GetGrain<IGameRoomGrain>(roomId).HasUserAsync(room.OwnerId);
        await messenger.SendAsync(moderatorId, new ModeratorRoomInfoComposer(
            room.Id, count, ownerIn, room.Owner?.AccountId ?? 0, room.Owner?.Username ?? string.Empty,
            true, room.Name, room.Description, room.Tags));
    }

    public async Task SendUserInfoAsync(Guid moderatorId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (user is null || !await MayAsync(db, moderatorId, PermissionNames.ModerationTool))
            return;

        var now = DateTimeOffset.UtcNow;
        var online = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync([user.Id])).Count > 0;
        var sanctions = await db.Sanctions.AsNoTracking().Where(s => s.UserId == user.Id).ToListAsync();
        var calls = db.ModerationIssues.Where(i => i.ReporterId == user.Id);
        var lastPurchase = await db.CurrencyTransactions
            .Where(t => t.UserId == user.Id && t.Reason == CurrencyTransactionReasons.CatalogPurchase)
            .MaxAsync(t => (DateTimeOffset?)t.CreatedAt);
        var machineBans = user.MachineId.Length == 0
            ? 0
            : await db.Sanctions.CountAsync(s => s.Kind == SanctionKind.Ban && s.MachineId == user.MachineId);
        var last = sanctions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

        await messenger.SendAsync(moderatorId, new ModeratorUserInfoComposer(
            user.AccountId,
            user.Username,
            user.Figure,
            (int)(now - user.CreatedAt).TotalMinutes,
            user.LastLoginAt is { } login ? (int)(now - login).TotalMinutes : 0,
            online,
            await calls.CountAsync(),
            await calls.CountAsync(i => i.CloseReason == ModerationCloseReason.Abusive),
            sanctions.Count(s => s.Kind == SanctionKind.Alert),
            sanctions.Count(s => s.Kind == SanctionKind.Ban),
            sanctions.Count(s => s.Kind == SanctionKind.TradeLock),
            user.TradeLockedUntil is { } locked && locked > now ? Date(locked) : Never,
            lastPurchase is { } purchased ? Date(purchased) : Never,
            user.AccountId,
            machineBans,
            user.Email,
            ClassificationOf(user),
            last is null ? Never : Date(last.CreatedAt),
            last is null ? 0 : (int)(now - last.CreatedAt).TotalHours));
    }

    /// <summary>What the mod tool shows a user as.</summary>
    private string ClassificationOf(User user) =>
        permissions.Has(user, PermissionNames.ModerationTool) ? StaffClassification
        : user.IsAmbassador ? AmbassadorClassification
        : user.ClubLevel > ClubLevel.None ? ClubClassification
        : RegularClassification;

    private const string StaffClassification = "staff";
    private const string AmbassadorClassification = "ambassador";
    private const string ClubClassification = "club";
    private const string RegularClassification = "regular";

    public async Task SanctionAsync(Guid moderatorId, ModerationAction action)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var required = action.Kind switch
        {
            SanctionKind.Ban => PermissionNames.ModerationBan,
            SanctionKind.Kick => PermissionNames.ModerationKick,
            _ => PermissionNames.ModerationAlert,
        };
        var target = await db.Users.FirstOrDefaultAsync(u => u.AccountId == action.AccountId);
        if (target is null || !await MayAsync(db, moderatorId, required) || target.Rank >= await RankOfAsync(db, moderatorId))
        {
            await messenger.SendAsync(moderatorId, new ModeratorActionResultMessageComposer(action.AccountId, false));
            return;
        }

        DateTimeOffset? expires = null;
        int? typeId = null;
        switch (action.Kind)
        {
            case SanctionKind.Mute:
            {
                var mute = await db.SanctionTypes.AsNoTracking().Where(t => t.Kind == SanctionKind.Mute).OrderBy(t => t.Step).FirstOrDefaultAsync();
                typeId = mute?.Id;
                expires = DateTimeOffset.UtcNow.AddHours(mute?.LengthHours ?? 0);
                break;
            }
            case SanctionKind.Ban:
            {
                var ban = await db.SanctionTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == action.SanctionTypeId && t.Kind == SanctionKind.Ban);
                if (ban is null)
                {
                    await messenger.SendAsync(moderatorId, new ModeratorActionResultMessageComposer(action.AccountId, false));
                    return;
                }

                typeId = ban.Id;
                expires = DateTimeOffset.UtcNow.AddHours(ban.LengthHours);
                break;
            }
            case SanctionKind.TradeLock:
                expires = DateTimeOffset.UtcNow.AddMinutes(Math.Max(0, action.Minutes ?? 0));
                break;
        }

        await ApplyAsync(db, moderatorId, target, action.Kind, typeId, action.TopicId, action.Message, action.IssueId, expires, action.AvatarOnly);
        await messenger.SendAsync(moderatorId, new ModeratorActionResultMessageComposer(action.AccountId, true));
    }

    public Task DefaultSanctionAsync(Guid moderatorId, int accountId, int topicId, string message, int? issueId) =>
        TryDefaultSanctionAsync(moderatorId, accountId, topicId, message, issueId);

    /// <summary>Gives the user the next sanction of the ladder; false when the moderator may not (rank, permission).</summary>
    private async Task<bool> TryDefaultSanctionAsync(Guid moderatorId, int accountId, int topicId, string message, int? issueId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var target = await db.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (target is null || !await MayAsync(db, moderatorId, PermissionNames.ModerationAlert) || target.Rank >= await RankOfAsync(db, moderatorId))
            return false;

        var next = await NextSanctionTypeAsync(db, target.Id);
        if (next.Kind == SanctionKind.Ban && !await MayAsync(db, moderatorId, PermissionNames.ModerationBan))
            return false;

        var expires = next.LengthHours > 0 ? DateTimeOffset.UtcNow.AddHours(next.LengthHours) : (DateTimeOffset?)null;
        await ApplyAsync(db, moderatorId, target, next.Kind, next.Id, topicId, message, issueId, expires, avatarOnly: false);
        await messenger.SendAsync(moderatorId, new ModeratorActionResultMessageComposer(accountId, true));
        return true;
    }

    /// <summary>The next step of the ladder for a user: one up from their last laddered sanction
    /// while they are still on probation for it, the first step otherwise.</summary>
    private static async Task<SanctionType> NextSanctionTypeAsync(AppDbContext db, Guid userId)
    {
        var ladder = await db.SanctionTypes.AsNoTracking().OrderBy(t => t.Step).ToListAsync();
        var last = await db.Sanctions.AsNoTracking()
            .Where(s => s.UserId == userId && s.SanctionTypeId != null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
        var lastType = last is null ? null : ladder.FirstOrDefault(t => t.Id == last.SanctionTypeId);
        if (last is null || lastType is null
            || (last.ExpiresAt ?? last.CreatedAt).AddDays(lastType.ProbationDays) < DateTimeOffset.UtcNow)
            return ladder.First();

        return ladder.FirstOrDefault(t => t.Step > lastType.Step) ?? lastType;
    }

    /// <summary>Records a sanction and makes it bite: the user is told, muted, sent out of their
    /// room, locked out of trading or banned and disconnected.</summary>
    private async Task ApplyAsync(
        AppDbContext db, Guid moderatorId, User target, SanctionKind kind, int? typeId, int topicId, string message,
        int? issueId, DateTimeOffset? expires, bool avatarOnly)
    {
        db.Sanctions.Add(new Sanction
        {
            UserId = target.Id,
            Kind = kind,
            SanctionTypeId = typeId,
            TopicId = topicId,
            Message = message.Length > ModerationIssue.MessageMaxLength ? message[..ModerationIssue.MessageMaxLength] : message,
            ModeratorId = moderatorId,
            IssueId = issueId,
            ExpiresAt = expires,
            AvatarOnly = avatarOnly,
            MachineId = kind == SanctionKind.Ban && !avatarOnly ? target.MachineId : string.Empty,
        });

        switch (kind)
        {
            case SanctionKind.Mute:
                target.MutedUntil = expires;
                break;
            case SanctionKind.TradeLock:
                target.TradeLockedUntil = expires;
                break;
        }

        await db.SaveChangesAsync();

        var player = grains.GetGrain<IPlayerGrain>(target.Id);
        var roomId = await player.GetCurrentRoomAsync();
        switch (kind)
        {
            case SanctionKind.Alert:
                await messenger.SendAsync(target.Id, new ModeratorCautionComposer(message, NoText));
                break;
            case SanctionKind.Message:
                // a message from an issue to the one who reported it answers their call for help
                var answersCall = issueId is { } answered
                                  && await db.ModerationIssues.AnyAsync(i => i.Id == answered && i.ReporterId == target.Id);
                await messenger.SendAsync(target.Id, answersCall
                    ? new Habbo.Protocol.Messages.Outgoing.Help.CallForHelpReplyMessageComposer(message)
                    : new ModeratorMessageComposer(message, NoText));
                break;
            case SanctionKind.Mute:
            case SanctionKind.TradeLock:
            {
                var data = await player.ReloadAsync();
                if (message.Length > 0)
                    await messenger.SendAsync(target.Id, new ModeratorCautionComposer(message, NoText));
                if (roomId is { } room)
                    await grains.GetGrain<IGameRoomGrain>(room).RefreshPlayerAsync(target.Id, data);
                break;
            }
            case SanctionKind.Kick:
                if (message.Length > 0)
                    await messenger.SendAsync(target.Id, new ModeratorCautionComposer(message, NoText));
                if (roomId is { } kickedFrom)
                    await grains.GetGrain<IGameRoomGrain>(kickedFrom).StaffKickAsync(target.Id);
                break;
            case SanctionKind.Ban:
                if (roomId is { } bannedFrom)
                    await grains.GetGrain<IGameRoomGrain>(bannedFrom).StaffKickAsync(target.Id);
                await messenger.SendAsync(target.Id, new UserBannedMessageComposer(message));
                await player.DisconnectAsync(Habbo.Protocol.Messages.Outgoing.Handshake.DisconnectReasonComposer.JustBanned);
                break;
        }
    }

    public async Task RoomActionAsync(Guid moderatorId, int actionType, string message)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var kick = actionType is RoomCautionAndKick or RoomMessageAndKick;
        if (!await MayAsync(db, moderatorId, kick ? PermissionNames.ModerationRoomKick : PermissionNames.ModerationRoomAlert)
            || await grains.GetGrain<IPlayerGrain>(moderatorId).GetCurrentRoomAsync() is not { } roomId)
            return;

        var caution = actionType is RoomCaution or RoomCautionAndKick;
        if (actionType is RoomCaution or RoomCautionAndKick or RoomMessage or RoomMessageAndKick)
            await grains.GetGrain<IGameRoomGrain>(roomId).StaffMessageAsync(message, caution, kick);
    }

    public async Task ModerateRoomAsync(Guid moderatorId, int roomId, bool lockDoor, bool changeTitle, bool kickUsers)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, kickUsers ? PermissionNames.ModerationRoomKick : PermissionNames.ModerationRoomAlert)
            || !await db.Rooms.AnyAsync(r => r.Id == roomId))
            return;

        await grains.GetGrain<IGameRoomGrain>(roomId).StaffModerateAsync(lockDoor, changeTitle ? rules.ModeratedRoomName : null, kickUsers);
    }

    public async Task SavePreferencesAsync(Guid moderatorId, int x, int y, int width, int height)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await MayAsync(db, moderatorId, PermissionNames.ModerationTool))
            return;

        var preference = await db.ModToolPreferences.FirstOrDefaultAsync(p => p.UserId == moderatorId);
        if (preference is null)
        {
            preference = new ModToolPreference { UserId = moderatorId };
            db.ModToolPreferences.Add(preference);
        }

        (preference.WindowX, preference.WindowY, preference.WindowWidth, preference.WindowHeight) = (x, y, width, height);
        await db.SaveChangesAsync();
    }

    /// <summary>When each player's calls this hour began, and how many they made: kept apart from the issues, since a
    /// call deleted and made again is gone from those.</summary>
    private readonly ConcurrentDictionary<Guid, (DateTimeOffset Since, int Calls)> _calls = new();

    /// <summary>The reporter's chat evidence goes back this far: what they report was said lately.</summary>
    private static readonly TimeSpan EvidenceWindow = TimeSpan.FromDays(1);

    /// <summary>Counts a call; false when the player has made as many this hour as they may.</summary>
    private bool TakeCall(Guid reporterId)
    {
        var now = DateTimeOffset.UtcNow;
        var (since, calls) = _calls.AddOrUpdate(reporterId, (now, 1),
            (_, known) => now - known.Since > TimeSpan.FromHours(1) ? (now, 1) : (known.Since, known.Calls + 1));
        return calls <= options.Value.CallsPerHour;
    }

    public async Task ReportAsync(Guid reporterId, CallForHelpReport report)
    {
        var rules = options.Value;
        if (!rules.CallForHelpEnabled)
        {
            await messenger.SendAsync(reporterId, new CallForHelpDisabledNotifyMessageComposer(rules.CallForHelpInfoUrl));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        // one call at a time: an open one is shown back for the reporter to keep or drop
        var pending = await PendingQuery(db, reporterId).ToListAsync();
        if (pending.Count > 0)
        {
            await SendPendingAsync(reporterId, pending);
            return;
        }

        // an abusive call keeps the reporter quiet for a while, and nobody makes calls without end
        var cooldown = DateTimeOffset.UtcNow.AddMinutes(-rules.AbusiveCallCooldownMinutes);
        if (await db.ModerationIssues.AnyAsync(i => i.ReporterId == reporterId && i.CloseReason == ModerationCloseReason.Abusive && i.ClosedAt > cooldown)
            || !TakeCall(reporterId))
            return;

        // the chat the client says was said is kept only where the hotel heard it said
        var issue = await FileIssueAsync(db, reporterId, report with { ChatLines = await HeardAsync(db, reporterId, report) });
        await messenger.SendAsync(reporterId, new CallForHelpResultMessageComposer(ReportAccepted, NoText));
        await BroadcastIssuesAsync(db, [issue.Id]);
    }

    public async Task ForwardAsync(Guid reporterId, CallForHelpReport report)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var issue = await FileIssueAsync(db, reporterId, report);
        await BroadcastIssuesAsync(db, [issue.Id]);
    }

    /// <summary>The most chat lines a report is checked for; the client sends what the reporter picked.</summary>
    private const int MaxReportedLines = 100;

    /// <summary>The report's chat lines that were said: in its room by that user, or in the console between that user
    /// and the reporter, lately. A client could otherwise put words in anyone's mouth for the moderators to read.</summary>
    private static async Task<List<ReportedChatLine>> HeardAsync(AppDbContext db, Guid reporterId, CallForHelpReport report)
    {
        // each line is looked up on its own, so a report holds no more of them than a moderator would read
        var lines = report.ChatLines.Take(MaxReportedLines).ToList();
        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(u => accountIds.Contains(u.AccountId)).ToDictionaryAsync(u => u.AccountId, u => u.Id);
        var since = DateTimeOffset.UtcNow - EvidenceWindow;
        var heard = new List<ReportedChatLine>();
        foreach (var line in lines)
        {
            if (!users.TryGetValue(line.AccountId, out var userId))
                continue;

            var said = report.RoomId is > 0
                ? await db.RoomChatLines.AnyAsync(c => c.RoomId == report.RoomId && c.UserId == userId && c.Text == line.Text && c.SaidAt > since)
                : await db.ConsoleMessages.AnyAsync(c => c.Text == line.Text && c.SentAt > since
                    && ((c.SenderId == userId && c.RecipientId == reporterId) || (c.SenderId == reporterId && c.RecipientId == userId)));
            if (said)
                heard.Add(line);
        }

        return heard;
    }

    /// <summary>Files the report as an issue for the moderators, with the names of those in its chat.</summary>
    private static async Task<ModerationIssue> FileIssueAsync(AppDbContext db, Guid reporterId, CallForHelpReport report)
    {
        var reported = report.ReportedAccountId is { } accountId && accountId > 0
            ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId)
            : null;
        var names = await db.Users.AsNoTracking()
            .Where(u => report.ChatLines.Select(l => l.AccountId).Contains(u.AccountId))
            .ToDictionaryAsync(u => u.AccountId, u => u.Username);

        var issue = new ModerationIssue
        {
            Source = report.Source,
            TopicId = report.TopicId,
            ReporterId = reporterId,
            ReportedUserId = reported?.Id,
            RoomId = report.RoomId is > 0 ? report.RoomId : null,
            GroupId = report.GroupId,
            ThreadId = report.ThreadId,
            ForumMessageId = report.ForumMessageId,
            PhotoId = report.PhotoId,
            Message = report.Message.Length > ModerationIssue.MessageMaxLength ? report.Message[..ModerationIssue.MessageMaxLength] : report.Message,
            ReporterName = Clip(report.ReporterName, ReporterFieldMaxLength),
            ReporterEmail = Clip(report.ReporterEmail, ReporterFieldMaxLength),
            ChatLines = report.ChatLines.Select(l => new ModerationIssueChatLine
            {
                UserAccountId = l.AccountId,
                UserName = names.GetValueOrDefault(l.AccountId, string.Empty),
                Text = l.Text.Length > RoomChatLine.TextMaxLength ? l.Text[..RoomChatLine.TextMaxLength] : l.Text,
            }).ToList(),
        };
        db.ModerationIssues.Add(issue);
        await db.SaveChangesAsync();
        return issue;
    }

    /// <summary>The longest reporter name and e-mail the issue keeps (its columns' length).</summary>
    private const int ReporterFieldMaxLength = 128;

    private static string Clip(string text, int length) => text.Length > length ? text[..length] : text;

    public async Task SendPendingCallsAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await SendPendingAsync(playerId, await PendingQuery(db, playerId).ToListAsync());
    }

    private Task SendPendingAsync(Guid playerId, IEnumerable<ModerationIssue> pending) =>
        messenger.SendAsync(playerId, new CallForHelpPendingCallsMessageComposer(pending
            .Select(i => new PendingCallForHelpData(Key(i.Id), Date(i.CreatedAt), i.Message))
            .ToList()));

    private static IQueryable<ModerationIssue> PendingQuery(AppDbContext db, Guid playerId) =>
        db.ModerationIssues.AsNoTracking().Where(i => i.ReporterId == playerId && i.State != ModerationIssueState.Closed);

    public async Task DeletePendingCallsAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var pending = await db.ModerationIssues.Where(i => i.ReporterId == playerId && i.State == ModerationIssueState.Open).ToListAsync();
        db.ModerationIssues.RemoveRange(pending);
        await db.SaveChangesAsync();

        await messenger.SendAsync(playerId, new CallForHelpPendingCallsDeletedMessageComposer());
        foreach (var issue in pending)
            await messenger.SendAsync(await OnlineModeratorsAsync(db), new IssueDeletedMessageComposer(Key(issue.Id)));
    }

    public async Task SendMyReportStatusAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var issues = await db.ModerationIssues.AsNoTracking().Include(i => i.ReportedUser)
            .Where(i => i.ReporterId == playerId)
            .OrderByDescending(i => i.Id)
            .ToListAsync();

        await messenger.SendAsync(playerId, new MyCfhReportStatusMessageComposer(issues.Select(i => new CfhReportData(
                i.Id,
                Millis(i.CreatedAt),
                i.Message,
                i.TopicId,
                i.ReportedUser?.Username ?? string.Empty,
                Millis(i.ClosedAt),
                i.Sanctioned,
                false,
                (byte)i.AppealState,
                Millis(i.AppealedAt),
                Millis(i.AppealResolvedAt)))
            .ToList()));
    }

    public async Task SendMySanctionStatusAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ladder = await db.SanctionTypes.AsNoTracking().OrderBy(t => t.Step).ToListAsync();
        var sanctions = await db.Sanctions.AsNoTracking()
            .Where(s => s.UserId == playerId && s.SanctionTypeId != null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        var next = await NextSanctionTypeAsync(db, playerId);

        await messenger.SendAsync(playerId, new MySanctionStatusMessageComposer(sanctions.Take(1).Select(s =>
        {
            var type = ladder.First(t => t.Id == s.SanctionTypeId);
            var probationEnds = (s.ExpiresAt ?? s.CreatedAt).AddDays(type.ProbationDays);
            var daysLeft = (int)Math.Max(0, Math.Ceiling((probationEnds - DateTimeOffset.UtcNow).TotalDays));
            return new SanctionStatusData(
                ToTypeData(type), s.Message, daysLeft > 0, daysLeft, ToTypeData(next));
        }).ToList()));
    }

    private static SanctionTypeData ToTypeData(SanctionType type) => new(type.Name, type.LengthHours, type.ProbationDays);

    public async Task AppealAsync(Guid playerId, int issueId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var issue = await db.ModerationIssues.FirstOrDefaultAsync(i => i.Id == issueId && i.ReporterId == playerId
                                                                       && i.State == ModerationIssueState.Closed
                                                                       && i.AppealState == ModerationAppealState.None);
        if (issue is null)
            return;

        issue.AppealState = ModerationAppealState.Pending;
        issue.AppealedAt = DateTimeOffset.UtcNow;
        issue.State = ModerationIssueState.Open;
        await db.SaveChangesAsync();

        await BroadcastIssuesAsync(db, [issue.Id]);
        await SendMyReportStatusAsync(playerId);
    }

    /// <summary>Sends the issues as they stand now to every moderator online.</summary>
    private async Task BroadcastIssuesAsync(AppDbContext db, IEnumerable<int> issueIds)
    {
        var ids = issueIds.ToList();
        var moderators = await OnlineModeratorsAsync(db);
        foreach (var issue in await OpenIssuesQuery(db).Where(i => ids.Contains(i.Id)).ToListAsync())
            await messenger.SendAsync(moderators, new IssueInfoMessageComposer(ToIssueData(issue)));
    }

    private static IQueryable<ModerationIssue> OpenIssuesQuery(AppDbContext db) =>
        db.ModerationIssues.AsNoTracking()
            .Include(i => i.Reporter).Include(i => i.ReportedUser).Include(i => i.Picker)
            .Where(i => i.State != ModerationIssueState.Closed)
            .OrderBy(i => i.Id);

    private static IssueMessageData ToIssueData(ModerationIssue issue) => new(
        issue.Id,
        (int)issue.State,
        issue.Source,
        issue.TopicId,
        (int)Math.Min(int.MaxValue, (DateTimeOffset.UtcNow - issue.CreatedAt).TotalMilliseconds),
        issue.AppealState == ModerationAppealState.Pending ? 1 : 0,
        issue.ReportedUserId is null ? 0 : issue.ReportedUser!.AccountId,
        issue.Reporter?.AccountId ?? 0,
        issue.Reporter?.Username ?? string.Empty,
        issue.ReportedUser?.AccountId ?? 0,
        issue.ReportedUser?.Username ?? string.Empty,
        issue.Picker?.AccountId ?? 0,
        issue.Picker?.Username ?? string.Empty,
        issue.Message,
        issue.Id,
        []);

    private async Task<List<Guid>> OnlineModeratorsAsync(AppDbContext db)
    {
        // of those online, whoever may use the mod tool
        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();
        var ranks = await db.Users.AsNoTracking().Where(u => online.Contains(u.Id)).Select(u => new { u.Id, u.Rank }).ToListAsync();
        return ranks.Where(u => permissions.Has(u.Id, u.Rank, PermissionNames.ModerationTool)).Select(u => u.Id).ToList();
    }

    private static Task<SecurityLevel> RankOfAsync(AppDbContext db, Guid userId) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.Rank).FirstOrDefaultAsync();

    private async Task<bool> MayAsync(AppDbContext db, Guid userId, string permission) =>
        permissions.Has(userId, await RankOfAsync(db, userId), permission);

    private string Time(DateTimeOffset time) => time.UtcDateTime.ToString(options.Value.TimestampFormat, CultureInfo.InvariantCulture);

    /// <summary>How the mod tool's user info and the pending calls print a date.</summary>
    private const string DateFormat = "yyyy-MM-dd HH:mm";

    private static string Date(DateTimeOffset time) => time.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static long Millis(DateTimeOffset? time) => time?.ToUnixTimeMilliseconds() ?? 0;
}
