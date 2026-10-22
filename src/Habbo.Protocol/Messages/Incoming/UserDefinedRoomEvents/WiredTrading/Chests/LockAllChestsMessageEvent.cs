using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>LockAllChestsMessageEvent — lock or unlock the player's chests in the room, or lock every chest in it</summary>
[Incoming(ClientMessages.LockAllChestsMessageEvent)]
public sealed partial record LockAllChestsMessageEvent(bool Lock, bool AllChests);
