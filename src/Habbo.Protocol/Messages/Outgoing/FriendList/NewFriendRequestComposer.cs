using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>1137 NewFriendRequestComposer — somebody has asked to be friends, pushed the moment
/// they ask rather than waiting for the target to look.</summary>
[Message(ServerMessages.NewFriendRequestComposer)]
public sealed partial record NewFriendRequestComposer(FriendRequestData Request);
