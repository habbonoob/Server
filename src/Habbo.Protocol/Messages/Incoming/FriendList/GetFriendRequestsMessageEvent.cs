using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>508 GetFriendRequests — the client asks what is waiting to be answered.</summary>
[Incoming(ClientMessages.GetFriendRequestsMessageEvent)]
public sealed partial record GetFriendRequestsMessageEvent;
