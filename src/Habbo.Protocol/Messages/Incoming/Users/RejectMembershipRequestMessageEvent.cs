using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>RejectMembershipRequestMessageEvent</summary>
[Incoming(ClientMessages.RejectMembershipRequestMessageEvent)]
public sealed partial record RejectMembershipRequestMessageEvent(int GroupId, int UserId);
