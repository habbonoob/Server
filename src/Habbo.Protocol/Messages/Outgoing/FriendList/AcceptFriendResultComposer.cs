using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>AcceptFriendResultComposer</summary>
[Message(ServerMessages.AcceptFriendResultComposer)]
public sealed partial record AcceptFriendResultComposer(IReadOnlyList<AcceptFriendFailureData> Failures);
