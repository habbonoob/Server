using System.Globalization;
using Habbo.Domain.Groups;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Badges;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Groups;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Chat;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Groups;

/// <summary>Groups: making, running and joining them.</summary>
public sealed class GroupService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    WordFilter wordFilter,
    IOptions<GroupOptions> options) : IGroupService
{
    /// <summary>How the member list and the details print a date.</summary>
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>How many layers the client's badge editor has: it reads a setting for every one.</summary>
    private const int EditorLayers = 5;

    /// <summary>A layer with nothing on it: a part id no part has, in the middle of the badge.</summary>
    private static readonly GuildBadgeSettingsData EmptyLayer = new(PartId: 0, ColorId: 0, Position: 4);

    /// <summary>The client's rights level that lets members decorate the home room.</summary>
    private const int MembersDecorateLevel = 0;

    private const int AdminsDecorateLevel = 1;

    /// <summary>User groups are guilds; the client keeps the other kind for Habbo's own.</summary>
    private const bool IsGuild = true;

    /// <summary>The editor sends each badge layer as part, colour, position.</summary>
    private const int FieldsPerLayer = 3;

    public async Task SendCreationInfoAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await messenger.SendAsync(playerId, new GuildCreationInfoMessageComposer(
            options.Value.CreationCost, await AvailableRoomsAsync(db, playerId, null),
            EditorSettings(options.Value.DefaultBadge.Select(l => new GuildBadgeSettingsData(l.PartId, l.ColorId, l.Position)))));
    }

    /// <summary>A badge's layers as the editor reads them: one for each of its layers, the unused ones empty.</summary>
    private static List<GuildBadgeSettingsData> EditorSettings(IEnumerable<GuildBadgeSettingsData> layers)
    {
        var settings = layers.Take(EditorLayers).ToList();
        while (settings.Count < EditorLayers)
            settings.Add(EmptyLayer);
        return settings;
    }

    /// <summary>The player's rooms that are home to no group (plus, when editing, the group's own).</summary>
    private static async Task<List<RoomEntryData>> AvailableRoomsAsync(AppDbContext db, Guid ownerId, int? groupRoomId) =>
        await db.Rooms.AsNoTracking()
            .Where(r => r.OwnerId == ownerId && (r.Id == groupRoomId || !db.Groups.Any(g => g.RoomId == r.Id)))
            .OrderBy(r => r.Name)
            .Select(r => new RoomEntryData(r.Id, r.Name, db.RoomRights.Any(right => right.RoomId == r.Id)))
            .ToListAsync();

    public async Task SendEditorDataAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var parts = await db.GroupBadgeParts.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        var colors = await db.GroupColors.AsNoTracking().OrderBy(c => c.Id).ToListAsync();

        List<BadgePartData> Parts(GroupBadgePartKind kind) =>
            parts.Where(p => p.Kind == kind).Select(p => new BadgePartData(p.Id, p.FileName, p.MaskFileName)).ToList();
        List<GuildColorData> Colors(GroupColorKind kind) =>
            colors.Where(c => c.Kind == kind).Select(c => new GuildColorData(c.Id, c.Hex)).ToList();

        await messenger.SendAsync(playerId, new GuildEditorDataMessageComposer(
            Parts(GroupBadgePartKind.Base), Parts(GroupBadgePartKind.Symbol),
            Colors(GroupColorKind.Badge), Colors(GroupColorKind.Primary), Colors(GroupColorKind.Secondary)));
    }

    public async Task<bool> CreateAsync(Guid playerId, GroupDraft draft)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == playerId);

        int? failure = null;
        if (user.AccountSafetyLocked)
            failure = GroupEditFailReasons.SafetyLocked;
        else if (user.ClubLevel < rules.CreationClubLevel)
            failure = GroupEditFailReasons.ClubRequired;
        else if (!IsValidName(draft.Name) || !await wordFilter.IsCleanAsync(draft.Name))
            failure = GroupEditFailReasons.InvalidName;
        else if (!await db.Rooms.AnyAsync(r => r.Id == draft.RoomId && r.OwnerId == playerId)
                 || await db.Groups.AnyAsync(g => g.RoomId == draft.RoomId))
            failure = GroupEditFailReasons.RoomAlreadyHome;
        else if (await db.GroupMembers.CountAsync(m => m.UserId == playerId) >= MembershipLimit(user))
            failure = GroupEditFailReasons.TooManyGroups;

        if (failure is { } reason)
        {
            await messenger.SendAsync(playerId, new GuildEditFailedMessageComposer(reason));
            return false;
        }

        var layers = await ParseBadgeAsync(db, draft.BadgeData);
        if (layers is null || !await ColorsExistAsync(db, draft.PrimaryColorId, draft.SecondaryColorId))
            return false;

        var player = grains.GetGrain<IPlayerGrain>(playerId);
        if (await player.SpendCreditsAsync(rules.CreationCost, CurrencyTransactionReasons.GroupCreation) is null)
            return false;

        var group = new Group
        {
            Name = draft.Name.Trim(),
            Description = Clip((await wordFilter.FilterAsync(draft.Description.Trim())).Text, Group.DescriptionMaxLength),
            RoomId = draft.RoomId,
            OwnerId = playerId,
            PrimaryColorId = draft.PrimaryColorId,
            SecondaryColorId = draft.SecondaryColorId,
            BadgeLayers = layers,
            BadgeCode = GroupBadgeCode.Build(layers),
        };
        // the group and its owner's membership are made together, and whatever stops them gives the price back
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = playerId, Rank = GroupRank.Owner });
            var owner = await db.Users.FirstAsync(u => u.Id == playerId);
            owner.FavouriteGroupId ??= group.Id;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // the room became another group's home since it was checked (two created at once)
            await player.AddCreditsAsync(rules.CreationCost, CurrencyTransactionReasons.GroupCreation);
            await messenger.SendAsync(playerId, new GuildEditFailedMessageComposer(GroupEditFailReasons.RoomAlreadyHome));
            return false;
        }
        catch
        {
            await player.AddCreditsAsync(rules.CreationCost, CurrencyTransactionReasons.GroupCreation);
            throw;
        }

        await messenger.SendAsync(playerId, new GuildCreatedMessageComposer(group.RoomId, group.Id));
        await RoomGroupChangedAsync(group.RoomId);
        return true;
    }

    public async Task SendEditInfoAsync(Guid playerId, int groupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().Include(g => g.BadgeLayers).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null || await RankOfAsync(db, groupId, playerId) is not (GroupRank.Owner or GroupRank.Admin))
            return;

        await messenger.SendAsync(playerId, new GuildEditInfoMessageComposer(
            await AvailableRoomsAsync(db, group.OwnerId, group.RoomId),
            group.OwnerId == playerId,
            group.Id,
            group.Name,
            group.Description,
            group.RoomId,
            group.PrimaryColorId,
            group.SecondaryColorId,
            (int)group.State,
            group.MembersCanDecorate ? MembersDecorateLevel : AdminsDecorateLevel,
            group.Locked,
            group.Url,
            EditorSettings(group.BadgeLayers.OrderBy(l => l.Layer).Select(l => new GuildBadgeSettingsData(l.PartId, l.ColorId, l.Position))),
            group.BadgeCode,
            await db.GroupMembers.CountAsync(m => m.GroupId == groupId)));
    }

    public Task UpdateIdentityAsync(Guid playerId, int groupId, string name, string description) =>
        EditAsync(playerId, groupId, async (_, group) =>
        {
            if (!IsValidName(name) || !await wordFilter.IsCleanAsync(name))
            {
                await messenger.SendAsync(playerId, new GuildEditFailedMessageComposer(GroupEditFailReasons.InvalidName));
                return false;
            }

            group.Name = name.Trim();
            group.Description = Clip((await wordFilter.FilterAsync(description.Trim())).Text, Group.DescriptionMaxLength);
            return true;
        });

    public Task UpdateBadgeAsync(Guid playerId, int groupId, IReadOnlyList<int> badgeData) =>
        EditAsync(playerId, groupId, async (db, group) =>
        {
            if (await ParseBadgeAsync(db, badgeData) is not { } layers)
                return false;

            db.GroupBadgeLayers.RemoveRange(await db.GroupBadgeLayers.Where(l => l.GroupId == groupId).ToListAsync());
            foreach (var layer in layers)
                layer.GroupId = groupId;
            db.GroupBadgeLayers.AddRange(layers);
            group.BadgeCode = GroupBadgeCode.Build(layers);
            return true;
        });

    public Task UpdateColorsAsync(Guid playerId, int groupId, int primaryColorId, int secondaryColorId) =>
        EditAsync(playerId, groupId, async (db, group) =>
        {
            if (!await ColorsExistAsync(db, primaryColorId, secondaryColorId))
                return false;

            group.PrimaryColorId = primaryColorId;
            group.SecondaryColorId = secondaryColorId;
            return true;
        });

    public Task UpdateSettingsAsync(Guid playerId, int groupId, int guildType, int rightsLevel) =>
        EditAsync(playerId, groupId, (_, group) =>
        {
            if (!Enum.IsDefined(typeof(GroupState), guildType))
                return Task.FromResult(false);

            group.State = (GroupState)guildType;
            group.MembersCanDecorate = rightsLevel == MembersDecorateLevel;
            return Task.FromResult(true);
        });

    /// <summary>Applies an edit an owner or admin makes, unless staff have locked the group, and
    /// tells the room and the editor the group changed.</summary>
    private async Task EditAsync(Guid playerId, int groupId, Func<AppDbContext, Group, Task<bool>> edit)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null || group.Locked || await RankOfAsync(db, groupId, playerId) is not (GroupRank.Owner or GroupRank.Admin))
            return;

        if (await db.Users.AnyAsync(u => u.Id == playerId && u.AccountSafetyLocked))
        {
            await messenger.SendAsync(playerId, new GuildEditFailedMessageComposer(GroupEditFailReasons.SafetyLocked));
            return;
        }

        if (!await edit(db, group))
            return;

        await db.SaveChangesAsync();
        await messenger.SendAsync(playerId, new GroupDetailsChangedMessageComposer(groupId));
        await RoomGroupChangedAsync(group.RoomId);
    }

    public async Task DeactivateAsync(Guid playerId, int groupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == playerId);
        if (group is null)
            return;

        var members = await db.GroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId).ToListAsync();
        db.Groups.Remove(group);
        await db.SaveChangesAsync();

        await messenger.SendAsync(members, new HabboGroupDeactivatedMessageComposer(groupId));
        await RoomGroupChangedAsync(group.RoomId);
    }

    public async Task RoomDeletedAsync(int roomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.RoomId == roomId);
        if (group is null)
            return;

        // as a deactivation, but the room is not told: it is the one deleting itself, and waiting on it would deadlock
        var members = await db.GroupMembers.Where(m => m.GroupId == group.Id).Select(m => m.UserId).ToListAsync();
        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        await messenger.SendAsync(members, new HabboGroupDeactivatedMessageComposer(group.Id));
    }

    public async Task JoinAsync(Guid playerId, int groupId)
    {
        var rules = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == playerId);
        if (group is null || await RankOfAsync(db, groupId, playerId) is not null)
            return;

        int? failure = null;
        if (group.State == GroupState.Private || await db.GroupBlocks.AnyAsync(b => b.GroupId == groupId && b.UserId == playerId))
            failure = GroupJoinFailReasons.GroupClosed;
        else if (await db.GroupMembers.CountAsync(m => m.GroupId == groupId) >= rules.MemberLimit)
            failure = GroupJoinFailReasons.GroupFull;
        else if (await db.GroupMembers.CountAsync(m => m.UserId == playerId) >= MembershipLimit(user))
            failure = user.ClubLevel == ClubLevel.None ? GroupJoinFailReasons.ClubRequired : GroupJoinFailReasons.TooManyGroups;

        if (failure is { } reason)
        {
            await messenger.SendAsync(playerId, new HabboGroupJoinFailedMessageComposer(reason));
            return;
        }

        if (group.State == GroupState.Exclusive)
        {
            if (!await db.GroupMembershipRequests.AnyAsync(r => r.GroupId == groupId && r.UserId == playerId))
            {
                db.GroupMembershipRequests.Add(new GroupMembershipRequest { GroupId = groupId, UserId = playerId });
                await db.SaveChangesAsync();
                await messenger.SendAsync(
                    await AdminsAsync(db, groupId),
                    new GroupMembershipRequestedMessageComposer(groupId, ToMemberData(user, GroupMemberTypes.Pending, DateTimeOffset.UtcNow)));
            }
        }
        else
        {
            db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = playerId });
            await db.SaveChangesAsync();
            await RoomGroupChangedAsync(group.RoomId);
        }

        await SendDetailsAsync(playerId, groupId, openDetails: false);
    }

    public async Task KickAsync(Guid playerId, int groupId, int accountId, bool block)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
        var target = await db.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (group is null || target is null)
            return;

        var leaving = target.Id == playerId;
        var actorRank = await RankOfAsync(db, groupId, playerId);
        var member = await db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == target.Id);
        if (member is null)
        {
            if (!leaving)
                await messenger.SendAsync(playerId, new GuildMemberMgmtFailedMessageComposer(groupId, GroupMemberManagementFailReasons.NotMember));
            return;
        }

        // the owner stays; an admin may remove members, and only the owner may remove admins
        if (member.Rank == GroupRank.Owner
            || (!leaving && actorRank is not (GroupRank.Owner or GroupRank.Admin))
            || (!leaving && member.Rank == GroupRank.Admin && actorRank != GroupRank.Owner))
            return;

        db.GroupMembers.Remove(member);
        if (block && !leaving && !await db.GroupBlocks.AnyAsync(b => b.GroupId == groupId && b.UserId == target.Id))
            db.GroupBlocks.Add(new GroupBlock { GroupId = groupId, UserId = target.Id });
        if (target.FavouriteGroupId == groupId)
            target.FavouriteGroupId = null;
        await db.SaveChangesAsync();

        // what they built in the home room goes back to them
        await grains.GetGrain<IGameRoomGrain>(group.RoomId).ReturnFurniOfAsync(target.Id);
        await RoomGroupChangedAsync(group.RoomId);

        await SendDetailsAsync(target.Id, groupId, openDetails: false);
        if (!leaving)
            await SendMembersAsync(playerId, groupId, 0, string.Empty, block ? GroupMemberSearchTypes.Blocked : GroupMemberSearchTypes.All);
    }

    public async Task UnblockAsync(Guid playerId, int groupId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await RankOfAsync(db, groupId, playerId) is not (GroupRank.Owner or GroupRank.Admin))
            return;

        var block = await db.GroupBlocks.FirstOrDefaultAsync(b => b.GroupId == groupId && b.User!.AccountId == accountId);
        if (block is null)
            return;

        db.GroupBlocks.Remove(block);
        await db.SaveChangesAsync();
        await SendMembersAsync(playerId, groupId, 0, string.Empty, GroupMemberSearchTypes.Blocked);
    }

    public async Task SetAdminAsync(Guid playerId, int groupId, int accountId, bool admin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == playerId);
        if (group is null)
            return;

        var member = await db.GroupMembers.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.User!.AccountId == accountId);
        if (member is null)
        {
            await messenger.SendAsync(playerId, new GuildMemberMgmtFailedMessageComposer(groupId, GroupMemberManagementFailReasons.NotMember));
            return;
        }

        if (member.Rank == GroupRank.Owner)
            return;

        member.Rank = admin ? GroupRank.Admin : GroupRank.Member;
        await db.SaveChangesAsync();

        await messenger.SendAsync(playerId, new GuildMembershipUpdatedMessageComposer(groupId, ToMemberData(member)));
        await RoomGroupChangedAsync(group.RoomId);
    }

    public async Task ApproveRequestAsync(Guid playerId, int groupId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null || await RankOfAsync(db, groupId, playerId) is not (GroupRank.Owner or GroupRank.Admin))
            return;

        var applicant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        var request = applicant is null
            ? null
            : await db.GroupMembershipRequests.FirstOrDefaultAsync(r => r.GroupId == groupId && r.UserId == applicant.Id);
        if (applicant is null || request is null)
        {
            var isMember = applicant is not null && await RankOfAsync(db, groupId, applicant.Id) is not null;
            await messenger.SendAsync(playerId, new GuildMemberMgmtFailedMessageComposer(groupId,
                isMember ? GroupMemberManagementFailReasons.AlreadyAccepted : GroupMemberManagementFailReasons.AlreadyRejected));
            return;
        }

        if (await db.GroupMembers.CountAsync(m => m.UserId == applicant.Id) >= MembershipLimit(applicant))
        {
            await messenger.SendAsync(playerId, new HabboGroupJoinFailedMessageComposer(
                applicant.ClubLevel == ClubLevel.None ? GroupJoinFailReasons.ApplicantNotClub : GroupJoinFailReasons.ApplicantAtLimit));
            return;
        }

        // a group full by joining is full by approving too
        if (await db.GroupMembers.CountAsync(m => m.GroupId == groupId) >= options.Value.MemberLimit)
        {
            await messenger.SendAsync(playerId, new HabboGroupJoinFailedMessageComposer(GroupJoinFailReasons.GroupFull));
            return;
        }

        db.GroupMembershipRequests.Remove(request);
        var membership = new GroupMember { GroupId = groupId, UserId = applicant.Id };
        db.GroupMembers.Add(membership);
        await db.SaveChangesAsync();

        await messenger.SendAsync(playerId, new GuildMembershipUpdatedMessageComposer(
            groupId, ToMemberData(applicant, GroupMemberTypes.Member, membership.JoinedAt)));
        await SendDetailsAsync(applicant.Id, groupId, openDetails: false);
        await RoomGroupChangedAsync(group.RoomId);
    }

    public async Task RejectRequestAsync(Guid playerId, int groupId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await RankOfAsync(db, groupId, playerId) is not (GroupRank.Owner or GroupRank.Admin))
            return;

        var request = await db.GroupMembershipRequests.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.User!.AccountId == accountId);
        if (request is null)
        {
            await messenger.SendAsync(playerId, new GuildMemberMgmtFailedMessageComposer(groupId, GroupMemberManagementFailReasons.AlreadyRejected));
            return;
        }

        db.GroupMembershipRequests.Remove(request);
        await db.SaveChangesAsync();
        await messenger.SendAsync(playerId, new GuildMembershipRejectedMessageComposer(groupId, accountId));
        await SendDetailsAsync(request.UserId, groupId, openDetails: false);
    }

    public async Task ApproveAllRequestsAsync(Guid playerId, int groupId)
    {
        List<int> applicants;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (await RankOfAsync(db, groupId, playerId) is not (GroupRank.Owner or GroupRank.Admin))
                return;

            applicants = await db.GroupMembershipRequests.Where(r => r.GroupId == groupId)
                .OrderBy(r => r.RequestedAt)
                .Select(r => r.User!.AccountId)
                .ToListAsync();
        }

        foreach (var accountId in applicants)
            await ApproveRequestAsync(playerId, groupId, accountId);

        await SendMembersAsync(playerId, groupId, 0, string.Empty, GroupMemberSearchTypes.All);
    }

    public async Task SendMembersAsync(Guid playerId, int groupId, int pageIndex, string userNameFilter, int searchType)
    {
        var pageSize = options.Value.MembersPageSize;
        pageIndex = Math.Max(0, pageIndex);
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
            return;

        var manages = await RankOfAsync(db, groupId, playerId) is GroupRank.Owner or GroupRank.Admin;
        if (!manages && searchType > GroupMemberSearchTypes.Admins)
            searchType = GroupMemberSearchTypes.All;

        var filter = userNameFilter.Trim().ToLowerInvariant();
        List<MemberData> page;
        int total;

        switch (searchType)
        {
            case GroupMemberSearchTypes.Pending:
            {
                var query = db.GroupMembershipRequests.AsNoTracking().Include(r => r.User)
                    .Where(r => r.GroupId == groupId && r.User!.Username.ToLower().Contains(filter));
                total = await query.CountAsync();
                page = (await query.OrderBy(r => r.RequestedAt).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync())
                    .Select(r => ToMemberData(r.User!, GroupMemberTypes.Pending, r.RequestedAt)).ToList();
                break;
            }
            case GroupMemberSearchTypes.Blocked:
            {
                var query = db.GroupBlocks.AsNoTracking().Include(b => b.User)
                    .Where(b => b.GroupId == groupId && b.User!.Username.ToLower().Contains(filter));
                total = await query.CountAsync();
                page = (await query.OrderBy(b => b.BlockedAt).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync())
                    .Select(b => ToMemberData(b.User!, GroupMemberTypes.Blocked, b.BlockedAt)).ToList();
                break;
            }
            default:
            {
                var query = db.GroupMembers.AsNoTracking().Include(m => m.User)
                    .Where(m => m.GroupId == groupId && m.User!.Username.ToLower().Contains(filter));
                if (searchType == GroupMemberSearchTypes.Admins)
                    query = query.Where(m => m.Rank != GroupRank.Member);
                total = await query.CountAsync();
                page = (await query.OrderBy(m => m.Rank).ThenBy(m => m.JoinedAt).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync())
                    .Select(ToMemberData).ToList();
                break;
            }
        }

        await messenger.SendAsync(playerId, new GuildMembersMessageComposer(
            group.Id, group.Name, group.RoomId, group.BadgeCode, total, page, manages, pageSize, pageIndex, searchType, userNameFilter));
    }

    public async Task SendMembershipsAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await messenger.SendAsync(playerId, new GuildMembershipsMessageComposer(await GroupEntriesAsync(db, playerId)));
    }

    /// <summary>The groups a user is in, as the profile and the membership list show them.</summary>
    public static async Task<List<HabboGroupEntryData>> GroupEntriesAsync(AppDbContext db, Guid userId)
    {
        var favourite = await db.Users.Where(u => u.Id == userId).Select(u => u.FavouriteGroupId).FirstOrDefaultAsync();
        var groups = await db.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new
            {
                m.Group!.Id,
                m.Group.Name,
                m.Group.BadgeCode,
                m.Group.PrimaryColorId,
                m.Group.SecondaryColorId,
                OwnerAccountId = m.Group.Owner!.AccountId,
                m.Group.HasForum,
            })
            .ToListAsync();
        var colors = await db.GroupColors.AsNoTracking().Where(c => c.Kind != GroupColorKind.Badge).ToListAsync();

        string Hex(GroupColorKind kind, int id) => colors.FirstOrDefault(c => c.Kind == kind && c.Id == id)?.Hex ?? string.Empty;

        return groups.Select(g => new HabboGroupEntryData(
                g.Id, g.Name, g.BadgeCode, Hex(GroupColorKind.Primary, g.PrimaryColorId), Hex(GroupColorKind.Secondary, g.SecondaryColorId),
                g.Id == favourite, g.OwnerAccountId, g.HasForum))
            .ToList();
    }

    public async Task SendDetailsAsync(Guid playerId, int groupId, bool openDetails)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.Groups.AsNoTracking().Include(g => g.Room).Include(g => g.Owner).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
            return;

        var rank = await RankOfAsync(db, groupId, playerId);
        var pending = await db.GroupMembershipRequests.AnyAsync(r => r.GroupId == groupId && r.UserId == playerId);
        var status = rank is not null ? GroupMembershipStatus.Member
            : pending ? GroupMembershipStatus.Pending
            : GroupMembershipStatus.None;
        var manages = rank is GroupRank.Owner or GroupRank.Admin;
        var favourite = await db.Users.AnyAsync(u => u.Id == playerId && u.FavouriteGroupId == groupId);

        await messenger.SendAsync(playerId, new HabboGroupDetailsMessageComposer(
            group.Id,
            IsGuild,
            (int)group.State,
            group.Name,
            group.Description,
            group.BadgeCode,
            group.RoomId,
            group.Room?.Name ?? string.Empty,
            status,
            await db.GroupMembers.CountAsync(m => m.GroupId == groupId),
            favourite,
            group.CreatedAt.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            rank == GroupRank.Owner,
            rank == GroupRank.Admin,
            group.Owner?.Username ?? string.Empty,
            openDetails,
            group.MembersCanDecorate,
            manages ? await db.GroupMembershipRequests.CountAsync(r => r.GroupId == groupId) : 0,
            group.HasForum));
    }

    public async Task SendMemberFurniCountAsync(Guid playerId, int groupId, int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var roomId = await db.Groups.Where(g => g.Id == groupId).Select(g => (int?)g.RoomId).FirstOrDefaultAsync();
        if (roomId is null)
            return;

        var count = await db.UserItems.CountAsync(i => i.RoomId == roomId && i.Owner!.AccountId == accountId);
        await messenger.SendAsync(playerId, new GuildMemberFurniCountInHQMessageComposer(accountId, count));
    }

    public async Task SetFavouriteAsync(Guid playerId, int? groupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (groupId is { } id && await RankOfAsync(db, id, playerId) is null)
            return;

        var user = await db.Users.FirstAsync(u => u.Id == playerId);
        user.FavouriteGroupId = groupId;
        await db.SaveChangesAsync();
        await SendMembershipsAsync(playerId);
    }

    private static Task<GroupRank?> RankOfAsync(AppDbContext db, int groupId, Guid userId) =>
        db.GroupMembers.Where(m => m.GroupId == groupId && m.UserId == userId).Select(m => (GroupRank?)m.Rank).FirstOrDefaultAsync();

    private static Task<List<Guid>> AdminsAsync(AppDbContext db, int groupId) =>
        db.GroupMembers.Where(m => m.GroupId == groupId && m.Rank != GroupRank.Member).Select(m => m.UserId).ToListAsync();

    private int MembershipLimit(User user) =>
        user.ClubLevel == ClubLevel.None ? options.Value.MembershipLimit : options.Value.ClubMembershipLimit;

    private bool IsValidName(string name) =>
        name.Trim().Length >= options.Value.NameMinLength && name.Trim().Length <= Group.NameMaxLength;

    private static string Clip(string text, int length) => text.Length > length ? text[..length] : text;

    /// <summary>Reads the editor's part, colour, position triples: a base first, then up to four
    /// symbols, each a part and colour the editor offers in one of the nine positions.</summary>
    private static async Task<List<GroupBadgeLayer>?> ParseBadgeAsync(AppDbContext db, IReadOnlyList<int> data)
    {
        if (data.Count == 0 || data.Count % FieldsPerLayer != 0 || data.Count / FieldsPerLayer > GroupBadgeCode.MaxLayers)
            return null;

        var bases = await db.GroupBadgeParts.Where(p => p.Kind == GroupBadgePartKind.Base).Select(p => p.Id).ToListAsync();
        var symbols = await db.GroupBadgeParts.Where(p => p.Kind == GroupBadgePartKind.Symbol).Select(p => p.Id).ToListAsync();
        var colors = await db.GroupColors.Where(c => c.Kind == GroupColorKind.Badge).Select(c => c.Id).ToListAsync();

        var layers = new List<GroupBadgeLayer>();
        for (var i = 0; i < data.Count / FieldsPerLayer; i++)
        {
            var (partId, colorId, position) = (data[i * FieldsPerLayer], data[i * FieldsPerLayer + 1], data[i * FieldsPerLayer + 2]);
            var known = i == 0 ? bases : symbols;
            if (!known.Contains(partId) || !colors.Contains(colorId) || position is < 0 or >= GroupBadgeCode.Positions)
                return null;

            layers.Add(new GroupBadgeLayer { Layer = i, PartId = partId, ColorId = colorId, Position = position });
        }

        return layers;
    }

    private static async Task<bool> ColorsExistAsync(AppDbContext db, int primaryColorId, int secondaryColorId) =>
        await db.GroupColors.AnyAsync(c => c.Kind == GroupColorKind.Primary && c.Id == primaryColorId)
        && await db.GroupColors.AnyAsync(c => c.Kind == GroupColorKind.Secondary && c.Id == secondaryColorId);

    private static MemberData ToMemberData(GroupMember member) =>
        ToMemberData(member.User!, (int)member.Rank, member.JoinedAt);

    private static MemberData ToMemberData(User user, int type, DateTimeOffset since) =>
        new(type, user.AccountId, user.Username, user.Figure, since.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture));

    /// <summary>Tells the group's home room, if anyone is in it, that the group changed.</summary>
    private async Task RoomGroupChangedAsync(int roomId)
    {
        var population = await grains.GetGrain<IRoomPopulationGrain>(0).GetAsync();
        if (population.GetValueOrDefault(roomId) > 0)
            await grains.GetGrain<IGameRoomGrain>(roomId).GroupChangedAsync();
    }
}
