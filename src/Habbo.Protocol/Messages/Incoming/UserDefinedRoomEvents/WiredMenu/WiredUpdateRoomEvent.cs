using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>WiredUpdateRoomEvent — reload the room, rolling its furni back to how it loaded first if asked</summary>
[Incoming(ClientMessages.WiredUpdateRoomEvent)]
public sealed partial record WiredUpdateRoomEvent(bool RollBack);
