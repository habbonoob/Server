using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>GetBannedUsersFromRoomMessageEvent</summary>
[Incoming(ClientMessages.GetBannedUsersFromRoomMessageEvent)]
public sealed partial record GetBannedUsersFromRoomMessageEvent(int RoomId);
