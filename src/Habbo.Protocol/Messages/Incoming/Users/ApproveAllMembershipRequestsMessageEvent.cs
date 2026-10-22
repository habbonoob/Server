using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>ApproveAllMembershipRequestsMessageEvent</summary>
[Incoming(ClientMessages.ApproveAllMembershipRequestsMessageEvent)]
public sealed partial record ApproveAllMembershipRequestsMessageEvent(int GroupId);
