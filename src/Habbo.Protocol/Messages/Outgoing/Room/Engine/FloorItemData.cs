using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>A floor item as Objects, ObjectAdd and ObjectUpdate carry it. The owner name is
/// only present on ObjectAdd, so it is optional here.</summary>
[WireStruct]
public sealed partial record FloorItemData(
    int ItemId,
    int ClassId,
    int X,
    int Y,
    int Rotation,
    string Z,
    string SizeZ,
    int Extra,
    FurniStuffData StuffData,
    int ExpiryTime,
    int UsagePolicy,
    int OwnerAccountId,
    string? OwnerName)
{
    public static FloorItemData From(RoomItemData item, bool withOwnerName) => new(
        item.ItemId,
        item.ClassId,
        item.X,
        item.Y,
        item.Rotation,
        item.Z,
        item.SizeZ,
        item.Extra,
        FurniStuffData.From(item.Data),
        item.ExpiryTime,
        item.UsagePolicy,
        item.OwnerAccountId,
        withOwnerName ? item.OwnerName : null);
}
