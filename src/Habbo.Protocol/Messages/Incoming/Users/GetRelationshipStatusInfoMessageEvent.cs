using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1141 GetRelationshipStatusInfo — how many friends a user has of each relationship.</summary>
[Incoming(ClientMessages.GetRelationshipStatusInfoMessageEvent)]
public sealed partial record GetRelationshipStatusInfoMessageEvent(int UserId);
