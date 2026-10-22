using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModerateRoomMessageEvent</summary>
[Incoming(ClientMessages.ModerateRoomMessageEvent)]
public sealed partial record ModerateRoomMessageEvent(int RoomId, int LockDoor, int ChangeTitle, int KickUsers);
