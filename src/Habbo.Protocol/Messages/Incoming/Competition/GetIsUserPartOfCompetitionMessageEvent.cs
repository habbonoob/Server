using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>GetIsUserPartOfCompetitionMessageEvent — whether the player has entered a room, and which</summary>
[Incoming(ClientMessages.GetIsUserPartOfCompetitionMessageEvent)]
public sealed partial record GetIsUserPartOfCompetitionMessageEvent(string GoalCode);
