using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>StartAddingToChestMessageEvent — start depositing into a chest</summary>
[Incoming(ClientMessages.StartAddingToChestMessageEvent)]
public sealed partial record StartAddingToChestMessageEvent(int ChestId);
