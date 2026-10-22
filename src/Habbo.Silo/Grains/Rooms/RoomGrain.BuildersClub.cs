using Habbo.Domain.Furniture;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Builders Club furniture: borrowed from the warehouse straight into the room, never into an inventory.
/// Only the room's owner (or a group's admin in its room) builds with it. Whoever places it, it is the owner's
/// membership that lends it: it is theirs, and counts against their club limit ("The owner of this room has no
/// Builders Club"). On the free trial (no membership time left) a room with Builders Club furni is hidden from the
/// navigator, which the player agrees to before the first piece goes down, and no one may be visiting while they
/// build.</summary>
public sealed partial class RoomGrain
{
    public async Task PlaceBuildersClubFloorItemAsync(Guid playerId, BuildersClubPlacement placement, int x, int y, int rotation)
    {
        if (_room is null || _items is null || _entities is null || await OwnersMembershipAsync(playerId) is not { } club)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var definition = await LoadBorrowedDefinitionAsync(db, placement, ItemType.Floor);
        if (definition is null || !_items.CanPlaceFloorItem(0, definition, x, y, rotation, _entities.HasAvatarAt))
            return;
        if (_items.Items.Count(i => i.Item.Definition!.Type == ItemType.Floor) >= _room.FloorItemLimit)
        {
            SendTo(playerId, FurniLimitReached());
            return;
        }
        if (!BuildersClubFurni.HasRoomFor(await BuildersClubFurni.CountAsync(db, _room.OwnerId), club.FurniLimit)
            || !await AgreedToHideAsync(playerId, placement, club, new BuildersClubPlacementWarningMessageComposer(
                BuildersClubPlacementWarningMessageComposer.Floor, placement.PageId, placement.OfferId, placement.ExtraParam,
                x, y, rotation, string.Empty)))
            return;

        if (await BorrowAsync(db, playerId, definition, placement, club, borrowed =>
            {
                borrowed.X = x;
                borrowed.Y = y;
                borrowed.Z = _items.SurfaceAt(x, y);
                borrowed.Rotation = rotation;
            }) is not var (item, data))
            return;
        await FloorItemPlacedAsync(playerId, ShowPlacedFloorItem(item, data));
        await SendBuildersClubCountAsync(db, _room.OwnerId);
    }

    public async Task PlaceBuildersClubWallItemAsync(Guid playerId, BuildersClubPlacement placement, string wallLocation)
    {
        if (_room is null || _items is null || !WallLocation.TryParse(wallLocation, _model!, out var location)
            || await OwnersMembershipAsync(playerId) is not { } club)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var definition = await LoadBorrowedDefinitionAsync(db, placement, ItemType.Wall);
        if (definition is null)
            return;
        if (_items.Items.Count(i => i.Item.Definition!.Type == ItemType.Wall) >= _room.WallItemLimit)
        {
            SendTo(playerId, FurniLimitReached());
            return;
        }
        if (!BuildersClubFurni.HasRoomFor(await BuildersClubFurni.CountAsync(db, _room.OwnerId), club.FurniLimit)
            || !await AgreedToHideAsync(playerId, placement, club, new BuildersClubPlacementWarningMessageComposer(
                BuildersClubPlacementWarningMessageComposer.Wall, placement.PageId, placement.OfferId, placement.ExtraParam,
                0, 0, 0, wallLocation)))
            return;

        if (await BorrowAsync(db, playerId, definition, placement, club, borrowed => borrowed.WallPosition = location.ToString())
            is not var (item, data))
            return;
        ShowPlacedWallItem(item, data);
        await SendBuildersClubCountAsync(db, _room.OwnerId);
    }

    /// <summary>The owner's membership, if this player may build with it here: the room's owner, or a group's admin
    /// in its room; on the trial, only with no one else here but staff (the client's own checks, which it shows as
    /// the warehouse's errors).</summary>
    private async Task<Habbo.Silo.Abstractions.Subscriptions.BuildersClubView?> OwnersMembershipAsync(Guid playerId)
    {
        if (_entities!.Find(playerId) is not { } builder
            || !BuildersClubFurni.MayBuild(_room!.OwnerId == playerId, builder.ControllerLevel))
            return null;

        var club = await subscriptions.GetBuildersClubAsync(_room.OwnerId);
        var visitors = _entities.Entities.Count(entity => entity != builder && !IsStaff(entity));
        return BuildersClubFurni.MayBuildWithVisitors(BuildersClubFurni.IsTrial(club), visitors) ? club : null;
    }

    private static Task<ItemDefinition?> LoadBorrowedDefinitionAsync(Habbo.Infrastructure.AppDbContext db, BuildersClubPlacement placement, ItemType type) =>
        db.ItemDefinitions
            .Include(d => d.States)
            .Include(d => d.Directions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == placement.DefinitionId && d.Type == type);

    /// <summary>Whether the placement may go ahead: on the trial, the first Builders Club furni hides the room, so
    /// the player is asked and the placement held until they send it again, confirmed.</summary>
    private async Task<bool> AgreedToHideAsync(
        Guid playerId, BuildersClubPlacement placement, Habbo.Silo.Abstractions.Subscriptions.BuildersClubView club,
        BuildersClubPlacementWarningMessageComposer warning)
    {
        if (!BuildersClubFurni.IsTrial(club))
            return true;

        // read, not remembered: the owner's login unhides their rooms when their membership is back
        await using var db = await dbFactory.CreateDbContextAsync();
        _room!.HiddenByBuildersClub = await db.Rooms.Where(r => r.Id == _room.Id).Select(r => r.HiddenByBuildersClub).FirstAsync();
        if (_room.HiddenByBuildersClub)
            return true;
        if (!placement.Confirmed)
        {
            SendTo(playerId, warning);
            return false;
        }

        await db.Rooms.Where(r => r.Id == _room.Id).ExecuteUpdateAsync(set => set.SetProperty(r => r.HiddenByBuildersClub, true));
        _room.HiddenByBuildersClub = true;
        return true;
    }

    /// <summary>A new borrowed item, the owner's, already at its place in the room, with what it starts with; null
    /// when the owner's limit is reached. The owner's borrowing is counted and written under a lock of theirs, as
    /// their rooms place at the same time each on its own grain.</summary>
    private async Task<(UserItem Item, FurniDataView Data)?> BorrowAsync(
        Habbo.Infrastructure.AppDbContext db, Guid playerId, ItemDefinition definition, BuildersClubPlacement placement,
        Habbo.Silo.Abstractions.Subscriptions.BuildersClubView club, Action<UserItem> place)
    {
        var ownerId = _room!.OwnerId;
        UserItem item;
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({BorrowLockPrefix + ownerId}))");
            if (!BuildersClubFurni.HasRoomFor(await BuildersClubFurni.CountAsync(db, ownerId), club.FurniLimit))
                return null;

            item = new UserItem { OwnerId = ownerId, DefinitionId = definition.Id, BuildersClub = true, RoomId = _room.Id, Sellable = false };
            place(item);
            db.UserItems.Add(item);
            var builder = _entities!.Find(playerId)!;
            interactions.For(definition).Grant(new FurniGrant(db, item, definition, placement.ProductParam, placement.ExtraParam,
                builder.Player.Username, DateTime.UtcNow));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }


        // read back as the room reads its items: with the owner for the wire
        var loaded = await WithDefinitions(db.UserItems).FirstAsync(i => i.Id == item.Id);
        return (loaded, await furniData.LoadAsync(db, loaded));
    }

    /// <summary>A room with Builders Club furni in it takes no visitors while its owner has no membership time left
    /// (the trial, or a membership run out): "This room contains Builders Club items and the owner does not have
    /// Builders Club membership".</summary>
    private async Task<bool> LockedByBuildersClubAsync() =>
        _items?.Items.Any(item => item.Item.BuildersClub) == true
        && BuildersClubFurni.IsTrial(await subscriptions.GetBuildersClubAsync(_room!.OwnerId));

    /// <summary>"%USERNAME% could not visit this room, because it contains Builders Club items and you do not have
    /// Builders Club membership."</summary>
    private Task TellOwnerOfLockedVisitAsync(string visitor) =>
        GrainFactory.GetGrain<IPlayerGrain>(_room!.OwnerId).SendAsync(new NotificationDialogMessageComposer(VisitDeniedForOwner,
            [new NotificationParameterData(UsernameParameter, visitor)]).ToMessage());

    /// <summary>Keys the advisory lock a borrower's placements take, apart from any other lock on their id.</summary>
    private const string BorrowLockPrefix = "builders_club:";

    private const string VisitDeniedForOwner = "builders_club.visit_denied_for_owner";
    private const string UsernameParameter = "USERNAME";

    /// <summary>Tells a player how many Builders Club items they have out, wherever they are.</summary>
    private async Task SendBuildersClubCountAsync(Habbo.Infrastructure.AppDbContext db, Guid playerId) =>
        await GrainFactory.GetGrain<IPlayerGrain>(playerId)
            .SendAsync(new BuildersClubFurniCountMessageComposer(await BuildersClubFurni.CountAsync(db, playerId)).ToMessage());
}
