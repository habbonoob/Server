using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>ForwardToRandomCompetitionRoomMessageEvent — to a room entered in the competition, picked at random</summary>
[Incoming(ClientMessages.ForwardToRandomCompetitionRoomMessageEvent)]
public sealed partial record ForwardToRandomCompetitionRoomMessageEvent(string GoalCode);
