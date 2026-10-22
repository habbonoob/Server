using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>1070 FriendRequestsComposer — the requests waiting for the player. The total is sent
/// apart from the list because the hotel may cap how many are listed at once.</summary>
[Message(ServerMessages.FriendRequestsComposer)]
public sealed partial record FriendRequestsComposer(
    int TotalRequests,
    IReadOnlyList<FriendRequestData> Requests);
