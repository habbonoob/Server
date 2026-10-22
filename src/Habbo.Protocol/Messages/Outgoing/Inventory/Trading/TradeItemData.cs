using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradeItemData — an item offered in a trade</summary>
[WireStruct]
public sealed partial record TradeItemData(int ItemId, string ItemType, int RoomItemId, int ClassId, int Category, bool IsGroupable, FurniStuffData StuffData, int CreationDay, int CreationMonth, int CreationYear, [property: WireWhen("IsFloorItem")] int? Extra)
{
    public const string FloorType = "S";
    public const string WallType = "I";

    /// <summary>Only a floor item carries its extra.</summary>
    public bool IsFloorItem => ItemType == FloorType;
}
