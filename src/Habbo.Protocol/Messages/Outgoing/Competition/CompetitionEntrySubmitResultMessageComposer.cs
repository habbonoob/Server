using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Competition;

/// <summary>CompetitionEntrySubmitResultMessageComposer — where entering the room stands; with the furni the competition wants and those the room lacks</summary>
[Message(ServerMessages.CompetitionEntrySubmitResultMessageComposer)]
public sealed partial record CompetitionEntrySubmitResultMessageComposer(int GoalId, string GoalCode, int Result, IReadOnlyList<string> RequiredFurnis, IReadOnlyList<string> MissingFurnis);
