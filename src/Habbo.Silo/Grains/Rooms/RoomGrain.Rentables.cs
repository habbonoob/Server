using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Spaces for rent in the room: rented for a price and a while, during which the renter may put
/// down, move and pick up their own furniture on the space's tiles (or anywhere, for a space that rents out
/// the whole room); when the rent runs out or is ended, what they put there goes back to them.</summary>
public sealed partial class RoomGrain
{
    private const string RentReason = "rentable_space";

    /// <summary>The rented spaces, by the space's item id.</summary>
    private readonly Dictionary<int, RentedSpace> _rentals = [];

    private async Task LoadRentalsAsync(AppDbContext db)
    {
        var itemIds = _items!.Items.Where(i => i.Interaction is RentableSpaceInteraction).Select(i => i.Item.Id).ToList();
        var rentals = await db.RentableSpaceRentals.AsNoTracking().Include(r => r.Renter).Where(r => itemIds.Contains(r.ItemId)).ToListAsync();
        foreach (var rental in rentals)
            _rentals[rental.ItemId] = new RentedSpace(rental.RenterId, rental.Renter!.AccountId, rental.Renter.Username, rental.RentedUntil);
    }

    /// <summary>The tiles a space rents out: its own, or the whole room.</summary>
    private bool SpaceCovers(RoomItemStore.PlacedItem space, IEnumerable<(int X, int Y)> tiles) =>
        space.Item.Definition!.CustomParams == rentableOptions.Value.WholeRoomParameter
        || tiles.All(tile => RoomItemStore.Footprint(space).Contains(tile));

    /// <summary>Whether the player rents a space here that covers all of these tiles.</summary>
    private bool RentsTiles(Guid playerId, IEnumerable<(int X, int Y)> tiles)
    {
        var wanted = tiles.ToList();
        return _rentals.Any(r => r.Value.RenterId == playerId && _items?.Find(r.Key) is { } space && SpaceCovers(space, wanted));
    }

    private static IEnumerable<(int X, int Y)> TilesAt(Habbo.Domain.Furniture.ItemDefinition definition, int x, int y, int rotation)
    {
        var (width, height) = RoomItemStore.FootprintSize(definition, rotation);
        for (var dx = 0; dx < width; dx++)
        for (var dy = 0; dy < height; dy++)
            yield return (x + dx, y + dy);
    }

    /// <summary>Why the player may not rent the space; none when they may.</summary>
    private async Task<int> RentRefusalAsync(Guid playerId, RoomItemStore.PlacedItem space)
    {
        var rules = rentableOptions.Value;
        if (!rules.Enabled)
            return RentableSpaceRentFailedMessageComposer.Disabled;
        if (_rentals.ContainsKey(space.Item.Id))
            return RentableSpaceRentFailedMessageComposer.AlreadyRented;
        if (space.Item.OwnerId == playerId)
            return RentableSpaceRentFailedMessageComposer.NoPermission;
        if (_entities?.Find(playerId) is not { } renter)
            return RentableSpaceRentFailedMessageComposer.Generic;
        if (rules.ClubRequired && renter.Player.ClubLevel == ClubLevel.None)
            return RentableSpaceRentFailedMessageComposer.ClubRequired;

        if (rules.OneSpacePerPlayer)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            // a rent that has run out counts for nothing, whether or not its room was loaded to end it
            if (await db.RentableSpaceRentals.AnyAsync(r => r.RenterId == playerId && r.RentedUntil > DateTimeOffset.UtcNow))
                return RentableSpaceRentFailedMessageComposer.OnlyOneSpace;
        }

        return RentableSpaceRentFailedMessageComposer.None;
    }

    public async Task SendRentableSpaceStatusAsync(Guid playerId, int itemId)
    {
        if (_items?.Find(itemId) is not { Interaction: RentableSpaceInteraction } space)
            return;

        var rules = rentableOptions.Value;
        if (_rentals.TryGetValue(itemId, out var rented))
        {
            SendTo(playerId, new RentableSpaceStatusMessageComposer(true, RentableSpaceRentFailedMessageComposer.AlreadyRented, rented.RenterAccountId,
                rented.RenterName, (int)Math.Max(0, (rented.Until - DateTimeOffset.UtcNow).TotalSeconds), rules.PriceInCredits));
            return;
        }

        var refusal = await RentRefusalAsync(playerId, space);
        SendTo(playerId, new RentableSpaceStatusMessageComposer(false, refusal, 0, string.Empty, 0, rules.PriceInCredits));
    }

    public async Task RentSpaceAsync(Guid playerId, int itemId)
    {
        if (_items?.Find(itemId) is not { Interaction: RentableSpaceInteraction } space || _entities?.Find(playerId) is not { } renter)
            return;

        var rules = rentableOptions.Value;
        var refusal = await RentRefusalAsync(playerId, space);
        if (refusal == RentableSpaceRentFailedMessageComposer.None
            && await GrainFactory.GetGrain<IPlayerGrain>(playerId).SpendCreditsAsync(rules.PriceInCredits, RentReason) is null)
            refusal = RentableSpaceRentFailedMessageComposer.NotEnoughCredits;
        if (refusal != RentableSpaceRentFailedMessageComposer.None)
        {
            SendTo(playerId, new RentableSpaceRentFailedMessageComposer(refusal));
            return;
        }

        var until = DateTimeOffset.UtcNow.AddSeconds(rules.RentSeconds);
        await using var db = await dbFactory.CreateDbContextAsync();
        db.RentableSpaceRentals.Add(new RentableSpaceRental { ItemId = itemId, RenterId = playerId, RentedUntil = until });
        await db.SaveChangesAsync();

        _rentals[itemId] = new RentedSpace(playerId, renter.Player.AccountId, renter.Player.Username, until);
        SendTo(playerId, new RentableSpaceRentOkMessageComposer(rules.RentSeconds));
    }

    /// <summary>The space's owner, staff or the renter end the rent, with no money back.</summary>
    public async Task CancelRentAsync(Guid playerId, int itemId)
    {
        if (_items?.Find(itemId) is not { Interaction: RentableSpaceInteraction } space)
            return;

        if (!_rentals.TryGetValue(itemId, out var rented))
        {
            SendTo(playerId, new RentableSpaceRentFailedMessageComposer(RentableSpaceRentFailedMessageComposer.NotRented));
            return;
        }

        if (space.Item.OwnerId != playerId && rented.RenterId != playerId && !IsOwnerOrStaff(playerId))
        {
            SendTo(playerId, new RentableSpaceRentFailedMessageComposer(RentableSpaceRentFailedMessageComposer.NotRentedByYou));
            return;
        }

        await EndRentAsync(space);
        await SendRentableSpaceStatusAsync(playerId, itemId);
    }

    /// <summary>Ends a rent: the renter's furniture on the space goes back to them.</summary>
    private async Task EndRentAsync(RoomItemStore.PlacedItem space)
    {
        if (!_rentals.Remove(space.Item.Id, out var rented))
            return;

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await db.RentableSpaceRentals.Where(r => r.ItemId == space.Item.Id).ExecuteDeleteAsync();
        }

        var theirs = _items!.Items
            .Where(i => i.Item.OwnerId == rented.RenterId && i.Item.Id != space.Item.Id && SpaceCovers(space, RoomItemStore.Footprint(i)))
            .ToList();
        await ReturnToOwnersAsync(theirs, rented.RenterAccountId);
    }

    /// <summary>Rents that have run out end, now and then as the room ticks.</summary>
    private async Task ExpireRentalsAsync()
    {
        if (_rentals.Count == 0 || _items is null)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var itemId in _rentals.Where(r => r.Value.Until <= now).Select(r => r.Key).ToList())
        {
            if (_items.Find(itemId) is { } space)
                await EndRentAsync(space);
            else
            {
                // the space was picked up: its rent ends all the same
                _rentals.Remove(itemId);
                await using var db = await dbFactory.CreateDbContextAsync();
                await db.RentableSpaceRentals.Where(r => r.ItemId == itemId).ExecuteDeleteAsync();
            }
        }
    }
}
