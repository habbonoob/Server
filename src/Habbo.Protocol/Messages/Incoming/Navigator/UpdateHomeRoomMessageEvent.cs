using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>179 UpdateHomeRoom — the room to treat as home.</summary>
[Incoming(ClientMessages.UpdateHomeRoomMessageEvent)]
public sealed partial record UpdateHomeRoomMessageEvent(int RoomId);
