using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>WithdrawAllFromChestMessageEvent — empty a chest</summary>
[Incoming(ClientMessages.WithdrawAllFromChestMessageEvent)]
public sealed partial record WithdrawAllFromChestMessageEvent(int ChestId);
