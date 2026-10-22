using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Competition;

/// <summary>CompetitionVotingInfoMessageComposer — whether the player may vote for this room, and how many votes they have left today</summary>
[Message(ServerMessages.CompetitionVotingInfoMessageComposer)]
public sealed partial record CompetitionVotingInfoMessageComposer(int GoalId, string GoalCode, int ResultCode, int VotesRemaining);
