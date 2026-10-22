using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;

/// <summary>ChestStorageData — an item in a chest</summary>
[WireStruct]
public sealed partial record ChestStorageData(int ItemId, int LockState, long TransactionId, ChestItemTypeData ItemType, bool Groupable, int SpecialType, FurniStuffData StuffData, [property: WireWhen("IsFloorItem")] int? Extra)
{
    /// <summary>The state no transaction holds the item in.</summary>
    public const int Unlocked = 0;

    /// <summary>Only a floor item carries its extra.</summary>
    public bool IsFloorItem => !ItemType.IsWallItem;
}
