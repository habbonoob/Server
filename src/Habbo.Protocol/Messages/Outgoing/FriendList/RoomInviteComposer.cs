using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>RoomInviteComposer — a friend invites the player to their room</summary>
[Message(ServerMessages.RoomInviteComposer)]
public sealed partial record RoomInviteComposer(int SenderId, string MessageText);
