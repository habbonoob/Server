using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>ForwardToACompetitionRoomMessageEvent — to a room entered in the competition</summary>
[Incoming(ClientMessages.ForwardToACompetitionRoomMessageEvent)]
public sealed partial record ForwardToACompetitionRoomMessageEvent(string GoalCode, int RoomId);
