using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>2492 AcceptFriend — accept these requests, by the id of whoever asked.</summary>
[Incoming(ClientMessages.AcceptFriendMessageEvent)]
public sealed partial record AcceptFriendMessageEvent(IReadOnlyList<int> RequesterIds);
