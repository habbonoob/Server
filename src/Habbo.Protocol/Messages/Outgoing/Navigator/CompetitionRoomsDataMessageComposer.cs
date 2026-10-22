using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>CompetitionRoomsDataMessageComposer</summary>
[Message(ServerMessages.CompetitionRoomsDataMessageComposer)]
public sealed partial record CompetitionRoomsDataMessageComposer(int GoalId, int PageIndex, int PageCount);
