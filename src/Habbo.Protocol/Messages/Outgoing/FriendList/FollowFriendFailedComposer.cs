using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>FollowFriendFailedComposer</summary>
[Message(ServerMessages.FollowFriendFailedComposer)]
public sealed partial record FollowFriendFailedComposer(int ErrorCode);
