using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>OpenChestAndGetContentsMessageEvent — open a chest and see what is in it</summary>
[Incoming(ClientMessages.OpenChestAndGetContentsMessageEvent)]
public sealed partial record OpenChestAndGetContentsMessageEvent(int ChestId);
