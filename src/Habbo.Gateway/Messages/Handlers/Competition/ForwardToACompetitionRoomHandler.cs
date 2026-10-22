using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>ForwardToACompetitionRoomMessageEvent — to a room entered in the competition.</summary>
internal sealed class ForwardToACompetitionRoomHandler(IRoomCompetitionService service) : IMessageHandler<ForwardToACompetitionRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ForwardToACompetitionRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ForwardToEntryAsync(player, message.GoalCode, message.RoomId);
        return true;
    }
}
