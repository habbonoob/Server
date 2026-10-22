using Habbo.Domain.Groups;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room's group: the group it is home to, whose members may get rights here, and the
/// favourite groups its users show beside their names.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The group this room is home to, if any.</summary>
    private Group? _group;

    /// <summary>What each user here is to the room's group.</summary>
    private readonly Dictionary<Guid, GroupRank> _groupRanks = [];

    private GroupRank? GroupRankOf(Guid playerId) =>
        _groupRanks.TryGetValue(playerId, out var rank) ? rank : null;

    private async Task LoadGroupAsync(AppDbContext db)
    {
        _group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.RoomId == (int)this.GetPrimaryKeyLong());
        if (_wired is not null)
            _wired.GroupId = _group?.Id;
    }

    /// <summary>Reads what the user is to the room's group and which group they show off.</summary>
    private async Task LoadMembershipAsync(RoomEntityStore.RoomEntity entity)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        _groupRanks.Remove(entity.Player.Id);
        if (_group is not null
            && await db.GroupMembers.Where(m => m.GroupId == _group.Id && m.UserId == entity.Player.Id)
                .Select(m => (GroupRank?)m.Rank).FirstOrDefaultAsync() is { } rank)
            _groupRanks[entity.Player.Id] = rank;

        var favourite = await db.Users.AsNoTracking()
            .Where(u => u.Id == entity.Player.Id && u.FavouriteGroupId != null)
            .Select(u => new
            {
                Id = u.FavouriteGroupId!.Value,
                Name = db.Groups.Where(g => g.Id == u.FavouriteGroupId).Select(g => g.Name).FirstOrDefault(),
                Badge = db.Groups.Where(g => g.Id == u.FavouriteGroupId).Select(g => g.BadgeCode).FirstOrDefault(),
                Rank = db.GroupMembers.Where(m => m.GroupId == u.FavouriteGroupId && m.UserId == u.Id)
                    .Select(m => (GroupRank?)m.Rank).FirstOrDefault(),
            })
            .FirstOrDefaultAsync();

        entity.GroupIds = (await db.GroupMembers.AsNoTracking().Where(m => m.UserId == entity.Player.Id).Select(m => m.GroupId).ToListAsync())
            .ToHashSet();
        entity.WornBadges = await db.UserBadges.AsNoTracking()
            .Where(b => b.UserId == entity.Player.Id && b.Slot != Habbo.Domain.Users.UserBadge.NotWorn)
            .OrderBy(b => b.Slot).Select(b => b.Code).ToListAsync();

        entity.FavouriteGroup = favourite is { Name: not null, Rank: not null }
            ? new FavouriteGroup(favourite.Id, favourite.Name, favourite.Badge ?? string.Empty, (int)favourite.Rank.Value)
            : null;
    }

    public async Task GroupChangedAsync()
    {
        if (_entities is null)
            return;

        await using (var db = await dbFactory.CreateDbContextAsync())
            await LoadGroupAsync(db);

        foreach (var entity in _entities.Entities.ToList())
        {
            var before = entity.FavouriteGroup;
            await LoadMembershipAsync(entity);
            RefreshControl(entity.Player.Id);
            if (before != entity.FavouriteGroup)
                BroadcastFavourite(entity);
        }

        if (_group is not null)
            Broadcast(new HabboGroupBadgesMessageComposer([new GroupBadgeData(_group.Id, _group.BadgeCode)]));
    }

    public async Task FavouriteGroupChangedAsync(Guid playerId)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        await LoadMembershipAsync(entity);
        BroadcastFavourite(entity);
    }

    /// <summary>Shows the room the user's new favourite, and the badge to draw for it.</summary>
    private void BroadcastFavourite(RoomEntityStore.RoomEntity entity)
    {
        if (entity.FavouriteGroup is { } group)
            Broadcast(new HabboGroupBadgesMessageComposer([new GroupBadgeData(group.Id, group.BadgeCode)]));

        Broadcast(new FavoriteMembershipUpdateMessageComposer(
            entity.RoomIndex,
            entity.FavouriteGroup?.Id ?? FavouriteGroup.None,
            entity.FavouriteGroup?.Status ?? FavouriteGroup.None,
            entity.FavouriteGroup?.Name ?? string.Empty));
    }

    public Task SendGroupBadgesAsync(Guid playerId)
    {
        if (_entities is null)
            return Task.CompletedTask;

        var badges = _entities.Entities
            .Select(e => e.FavouriteGroup)
            .OfType<FavouriteGroup>()
            .Select(g => new GroupBadgeData(g.Id, g.BadgeCode))
            .Concat(_group is null ? [] : [new GroupBadgeData(_group.Id, _group.BadgeCode)])
            .DistinctBy(b => b.GroupId)
            .ToList();
        SendTo(playerId, new HabboGroupBadgesMessageComposer(badges));
        return Task.CompletedTask;
    }

    public async Task ReturnFurniOfAsync(Guid ownerId)
    {
        if (_items is null)
            return;

        await ReturnToOwnersAsync(_items.Items.Where(p => p.Item.OwnerId == ownerId).ToList(), _room?.Owner?.AccountId ?? 0);
    }

    /// <summary>The room's group as listings show it.</summary>
    private RoomGroup? RoomGroupInfo => _group is null ? null : new RoomGroup(_group.Id, _group.Name, _group.BadgeCode);
}
