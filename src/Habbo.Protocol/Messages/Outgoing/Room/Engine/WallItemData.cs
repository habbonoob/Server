using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>A wall item as Items, ItemAdd and ItemUpdate carry it. Wall item ids go out as
/// strings. The owner name is only present on ItemAdd.</summary>
[WireStruct]
public sealed partial record WallItemData(
    [property: WireFormat(WireType.IntegerString)] int ItemId,
    int ClassId,
    string WallPosition,
    /// <summary>A wall item carries a bare state string, not full item data — the client's wall
    /// parser reads one string where the floor parser reads a typed header.</summary>
    string State,
    int ExpiryTime,
    int UsagePolicy,
    int OwnerAccountId,
    string? OwnerName)
{
    public static WallItemData From(RoomItemData item, bool withOwnerName) => new(
        item.ItemId,
        item.ClassId,
        item.WallPosition,
        item.Data.StateText,
        item.ExpiryTime,
        item.UsagePolicy,
        item.OwnerAccountId,
        withOwnerName ? item.OwnerName : null);
}
