using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>GetModeratorRoomInfoMessageEvent</summary>
[Incoming(ClientMessages.GetModeratorRoomInfoMessageEvent)]
public sealed partial record GetModeratorRoomInfoMessageEvent(int RoomId);
