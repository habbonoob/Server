using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>SubmitRoomToCompetitionMessageEvent — entering the room the player is in: 0 ask, 1 accept the rules, 2 submit, 3 confirm</summary>
[Incoming(ClientMessages.SubmitRoomToCompetitionMessageEvent)]
public sealed partial record SubmitRoomToCompetitionMessageEvent(string GoalCode, int Step);
