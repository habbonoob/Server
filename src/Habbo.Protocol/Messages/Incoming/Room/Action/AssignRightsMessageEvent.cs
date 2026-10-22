using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>1818 AssignRights — the owner gives rights in their room to this user (by account id).</summary>
[Incoming(ClientMessages.AssignRightsMessageEvent)]
public sealed partial record AssignRightsMessageEvent(int UserId);
