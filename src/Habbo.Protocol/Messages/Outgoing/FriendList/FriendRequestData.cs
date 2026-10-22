using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>One pending request, as the client lists it.</summary>
[WireStruct]
public sealed partial record FriendRequestData(int RequesterId, string RequesterName, string Figure);
