using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Opening a present in the room: it is gone, what it held goes to its owner, and the first
/// piece of floor furniture it held stands where the present stood, if it fits there.</summary>
public sealed partial class RoomGrain
{
    /// <summary>What a present never holds.</summary>
    private const string NoPetFigure = "";

    public async Task OpenPresentAsync(Guid playerId, int itemId)
    {
        if (_entities?.Find(playerId) is null || _items?.Find(itemId) is not { Interaction: PresentInteraction or EcotronBoxInteraction } present
            || present.Item.OwnerId != playerId)
            return;

        var owner = GrainFactory.GetGrain<IPlayerGrain>(playerId);
        if (await owner.UnwrapGiftAsync(itemId) is not { } gift)
            return;

        var (x, y, rotation) = (present.X, present.Y, present.Rotation);
        var vacated = RoomItemStore.Footprint(present).ToList();
        // gone as a pick-up takes an item: wired that picked it forgets it, and whoever was on it settles
        _items.Remove(itemId);
        if (_wired is not null)
            await _wired.RemovedAsync(itemId);
        Broadcast(new ObjectRemoveMessageComposer(itemId, IsExpired: false, present.Item.Owner?.AccountId ?? 0, Delay: 0));
        BroadcastHeights(vacated);
        ResettleOn(vacated, []);

        var placedId = 0;
        if (gift.FirstIsFloorItem && gift.ItemIds.Count > 0)
        {
            await PlaceFloorItemAsync(playerId, gift.ItemIds[0], x, y, rotation);
            if (_items.Find(gift.ItemIds[0]) is not null)
                placedId = gift.ItemIds[0];
        }

        await owner.AnnounceItemsAsync(gift.ItemIds.Where(id => id != placedId).ToList());
        SendTo(playerId, new PresentOpenedMessageComposer(gift.ItemType, gift.ClassId, gift.ProductCode,
            placedId != 0 ? placedId : gift.ItemIds.FirstOrDefault(), gift.ItemType, placedId != 0, NoPetFigure));
    }
}
