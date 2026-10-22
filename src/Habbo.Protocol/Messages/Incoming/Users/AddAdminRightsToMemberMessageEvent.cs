using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>AddAdminRightsToMemberMessageEvent</summary>
[Incoming(ClientMessages.AddAdminRightsToMemberMessageEvent)]
public sealed partial record AddAdminRightsToMemberMessageEvent(int GroupId, int UserId);
