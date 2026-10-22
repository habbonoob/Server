using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3440 HeightMapUpdateMessageComposer — the tiles whose stacking state changed.
/// The client reads the count as a single signed byte: past 127 it reads a negative count and runs off the end of
/// the message, so more tiles go out as several messages (<see cref="Chunked"/>).</summary>
[Message(ServerMessages.HeightMapUpdateMessageComposer)]
public sealed partial record HeightMapUpdateMessageComposer(
    [property: WireCount(WireType.U8)] IReadOnlyList<HeightMapTileWireData> Tiles)
{
    /// <summary>Heights are sent as a fixed-point value: whole units are 256 apart.</summary>
    private const int HeightScale = 256;

    /// <summary>The most tiles one message carries.</summary>
    public const int MaxTiles = sbyte.MaxValue;

    /// <summary>One message for up to <see cref="MaxTiles"/> tiles; more are refused rather than cut short.</summary>
    public static HeightMapUpdateMessageComposer From(IReadOnlyList<HeightMapTileData> tiles) =>
        tiles.Count <= MaxTiles
            ? new(tiles.Select(Encode).ToList())
            : throw new ArgumentException($"{tiles.Count} tiles do not fit one message; use Chunked", nameof(tiles));

    /// <summary>As many messages as the tiles take, none for none.</summary>
    public static IEnumerable<HeightMapUpdateMessageComposer> Chunked(IReadOnlyList<HeightMapTileData> tiles) =>
        tiles.Chunk(MaxTiles).Select(chunk => From(chunk));

    private static HeightMapTileWireData Encode(HeightMapTileData tile)
    {
        var value = (int)(tile.Height * HeightScale);
        if (tile.StackingBlocked)
            value |= 1 << HeightMapMessageComposer.StackingBlockedBit;

        return new HeightMapTileWireData((byte)tile.X, (byte)tile.Y, (short)value);
    }
}
