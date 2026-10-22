using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>DeleteRoomMessageEvent</summary>
[Incoming(ClientMessages.DeleteRoomMessageEvent)]
public sealed partial record DeleteRoomMessageEvent(int RoomId);
