using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>747 RelationshipStatusInfoComposer.</summary>
[Message(ServerMessages.RelationshipStatusInfoComposer)]
public sealed partial record RelationshipStatusInfoComposer(int UserId, IReadOnlyList<RelationshipStatusInfoData> Relationships);
