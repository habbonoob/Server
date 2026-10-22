using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>WithdrawItemsFromChestMessageEvent — take items of one type out of a chest</summary>
[Incoming(ClientMessages.WithdrawItemsFromChestMessageEvent)]
public sealed partial record WithdrawItemsFromChestMessageEvent(int ChestId, bool IsWallItem, int TypeId, string PosterId, int Amount);
