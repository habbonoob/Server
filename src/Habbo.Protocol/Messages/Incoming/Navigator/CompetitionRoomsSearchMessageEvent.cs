using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>CompetitionRoomsSearchMessageEvent</summary>
[Incoming(ClientMessages.CompetitionRoomsSearchMessageEvent)]
public sealed partial record CompetitionRoomsSearchMessageEvent(int GoalId, int PageIndex);
