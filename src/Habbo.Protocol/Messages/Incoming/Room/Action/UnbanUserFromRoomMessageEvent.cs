using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>UnbanUserFromRoomMessageEvent</summary>
[Incoming(ClientMessages.UnbanUserFromRoomMessageEvent)]
public sealed partial record UnbanUserFromRoomMessageEvent(int UserId, int RoomId);
