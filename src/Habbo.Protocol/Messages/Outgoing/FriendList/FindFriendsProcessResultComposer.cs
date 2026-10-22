using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>FindFriendsProcessResultComposer</summary>
[Message(ServerMessages.FindFriendsProcessResultComposer)]
public sealed partial record FindFriendsProcessResultComposer(bool Success);
