using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>AcceptFriendFailureData — a friend request that could not be accepted</summary>
[WireStruct]
public sealed partial record AcceptFriendFailureData(int SenderId, int ErrorCode);
