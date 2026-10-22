using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>CloseChestMessageEvent — stop looking into a chest</summary>
[Incoming(ClientMessages.CloseChestMessageEvent)]
public sealed partial record CloseChestMessageEvent(int ChestId);
