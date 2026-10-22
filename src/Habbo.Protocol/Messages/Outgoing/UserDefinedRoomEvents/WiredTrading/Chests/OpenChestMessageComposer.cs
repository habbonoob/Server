using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>OpenChestMessageComposer — the client should open this chest</summary>
[Message(ServerMessages.OpenChestMessageComposer)]
public sealed partial record OpenChestMessageComposer(int ChestId);
