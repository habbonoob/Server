using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>CompetitionRoomsSearchMessageEvent — a page of a room competition's entries.</summary>
internal sealed class CompetitionRoomsSearchHandler(IRoomDirectoryService service) : IMessageHandler<CompetitionRoomsSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CompetitionRoomsSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchCompetitionAsync(player, message.GoalId, message.PageIndex);
        return true;
    }
}
