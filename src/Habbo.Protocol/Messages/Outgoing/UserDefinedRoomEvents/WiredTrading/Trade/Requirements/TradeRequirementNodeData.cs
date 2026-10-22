using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;

/// <summary>TradeRequirementNodeData — credits, or so many items of one kind</summary>
[WireStruct]
public sealed partial record TradeRequirementNodeData(byte Type, int Amount, [property: WireWhen("IsFurni")] ChestItemTypeData? ItemType)
{
    public const byte Coins = 0;
    public const byte Furni = 1;

    public bool IsFurni => Type == Furni;
}
