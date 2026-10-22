using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;

/// <summary>ChestItemTypeData — a kind of item a chest holds</summary>
[WireStruct]
public sealed partial record ChestItemTypeData(bool IsWallItem, int TypeId, string PosterId);
