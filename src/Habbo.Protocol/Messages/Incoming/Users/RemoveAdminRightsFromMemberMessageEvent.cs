using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>RemoveAdminRightsFromMemberMessageEvent</summary>
[Incoming(ClientMessages.RemoveAdminRightsFromMemberMessageEvent)]
public sealed partial record RemoveAdminRightsFromMemberMessageEvent(int GroupId, int UserId);
