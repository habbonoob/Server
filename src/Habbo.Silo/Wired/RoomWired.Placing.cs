using Habbo.Domain.Furniture;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>Wired's temporary furni: copies Place Furni puts in the room, which only Remove Furni takes away.</summary>
public sealed partial class RoomWired
{
    public Task<RoomItemStore.PlacedItem?> PlaceCopyAsync(RoomItemStore.PlacedItem template, FurniDataView data, int x, int y, double? z,
        int rotation)
    {
        var definition = template.Item.Definition!;
        // a copy stands where furniture may, but through whatever is there, as a wired move may
        if (definition.Type != ItemType.Floor
            || !items.CanPlaceFloorItem(0, definition, x, y, rotation, (_, _) => false, throughFurni: true)
            || !TakeMove() || !TakeWrite()
            || items.AddCopy(template, data, x, y, z is { } altitude ? Math.Clamp(Math.Round(altitude, 2), 0, RoomItemStore.MaxAltitude) : null,
                rotation) is not { } copy)
            return Task.FromResult<RoomItemStore.PlacedItem?>(null);

        _stacks = null;
        furniture.Broadcast(new ObjectAddMessageComposer(FloorItemData.From(RoomItemStore.ToWireData(copy), withOwnerName: true)));
        foreach (var update in HeightMapUpdateMessageComposer.Chunked(items.TileUpdates(RoomItemStore.Footprint(copy))))
            furniture.Broadcast(update);
        return Task.FromResult<RoomItemStore.PlacedItem?>(copy);
    }

    public async Task<bool> RemoveCopyAsync(RoomItemStore.PlacedItem copy)
    {
        if (!copy.Temporary || items.Find(copy.Item.Id) != copy || !TakeWrite())
            return false;

        await furniture.RemoveAsync(copy);
        _stacks = null;
        return true;
    }
}
