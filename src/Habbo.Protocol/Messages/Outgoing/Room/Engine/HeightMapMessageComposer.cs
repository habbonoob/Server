using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>982 HeightMapMessageComposer — the width, then one short per tile. The client derives
/// the room height by dividing the tile count by the width, so no height is sent.</summary>
[Message(ServerMessages.HeightMapMessageComposer)]
public sealed partial record HeightMapMessageComposer(
    int Width,
    [property: WireFormat(WireType.I16)] IReadOnlyList<short> Tiles)
{
    /// <summary>The bit the client reads a tile's stacking-blocked flag from: room.stacking_blocked_mask_bit in the
    /// external_variables it is served (15, as Habbo's; the client's own default of 14 applies only without it).
    /// Sent on bit 14, a blocked 0.85 read as a height of 64.85.</summary>
    public const int StackingBlockedBit = 15;

    /// <summary>Void tiles are sent as -1.</summary>
    private const short VoidTile = -1;

    /// <summary>Heights are sent as a fixed-point value: whole units are 256 apart.</summary>
    private const int HeightScale = 256;

    /// <summary>The room's tiles, each with the height anything placed there would rest at. A
    /// tile the room does not list is not part of it, and is sent as void.</summary>
    public static HeightMapMessageComposer From(int width, int height, IReadOnlyList<HeightMapTileData> roomTiles)
    {
        var byPosition = roomTiles.ToDictionary(tile => (tile.X, tile.Y));
        var tiles = new List<short>(width * height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                tiles.Add(EncodeTile(byPosition.GetValueOrDefault((x, y))));
        }

        return new HeightMapMessageComposer(width, tiles);
    }

    internal static short EncodeTile(HeightMapTileData? tile)
    {
        if (tile is null)
            return VoidTile;

        var value = (int)(tile.Height * HeightScale);
        if (tile.StackingBlocked)
            value |= 1 << StackingBlockedBit;
        return (short)value;
    }
}
