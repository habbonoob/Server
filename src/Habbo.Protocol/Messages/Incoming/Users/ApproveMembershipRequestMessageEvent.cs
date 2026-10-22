using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>ApproveMembershipRequestMessageEvent</summary>
[Incoming(ClientMessages.ApproveMembershipRequestMessageEvent)]
public sealed partial record ApproveMembershipRequestMessageEvent(int GroupId, int UserId);
