using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>ChestItemAmountData — so many items of one kind</summary>
[WireStruct]
public sealed partial record ChestItemAmountData(ChestItemTypeData ItemType, int Amount);
