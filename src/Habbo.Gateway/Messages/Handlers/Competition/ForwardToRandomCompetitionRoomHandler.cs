using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>ForwardToRandomCompetitionRoomMessageEvent — to a room entered, picked at random.</summary>
internal sealed class ForwardToRandomCompetitionRoomHandler(IRoomCompetitionService service) : IMessageHandler<ForwardToRandomCompetitionRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ForwardToRandomCompetitionRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ForwardToRandomEntryAsync(player, message.GoalCode);
        return true;
    }
}
