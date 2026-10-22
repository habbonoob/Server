using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>GetRoomChatlogMessageEvent</summary>
[Incoming(ClientMessages.GetRoomChatlogMessageEvent)]
public sealed partial record GetRoomChatlogMessageEvent(int IsPublicRoom, int RoomId);
