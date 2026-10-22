using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>SendRoomInviteMessageEvent — invite friends to the player's room</summary>
[Incoming(ClientMessages.SendRoomInviteMessageEvent)]
public sealed partial record SendRoomInviteMessageEvent(IReadOnlyList<int> UserIds, string Message);
