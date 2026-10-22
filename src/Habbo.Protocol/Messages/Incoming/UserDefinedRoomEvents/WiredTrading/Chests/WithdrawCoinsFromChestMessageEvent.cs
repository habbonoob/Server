using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>WithdrawCoinsFromChestMessageEvent — take credits out of a chest</summary>
[Incoming(ClientMessages.WithdrawCoinsFromChestMessageEvent)]
public sealed partial record WithdrawCoinsFromChestMessageEvent(int ChestId, int Amount);
