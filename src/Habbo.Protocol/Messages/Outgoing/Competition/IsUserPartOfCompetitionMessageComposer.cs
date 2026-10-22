using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Competition;

/// <summary>IsUserPartOfCompetitionMessageComposer — whether the player has entered a room, and which</summary>
[Message(ServerMessages.IsUserPartOfCompetitionMessageComposer)]
public sealed partial record IsUserPartOfCompetitionMessageComposer(bool IsPartOf, int TargetId);
