using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>GetCompetitionVotingInfoMessageEvent — on going into a room: the competition as it concerns this room and player</summary>
[Incoming(ClientMessages.GetCompetitionVotingInfoMessageEvent)]
public sealed partial record GetCompetitionVotingInfoMessageEvent;
