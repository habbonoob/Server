using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>FollowFriendMessageEvent — go to the room a friend is in</summary>
[Incoming(ClientMessages.FollowFriendMessageEvent)]
public sealed partial record FollowFriendMessageEvent(int UserId);
