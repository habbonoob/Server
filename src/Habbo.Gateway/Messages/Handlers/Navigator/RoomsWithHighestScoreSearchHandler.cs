using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RoomsWithHighestScoreSearchMessageEvent — the best liked rooms.</summary>
internal sealed class RoomsWithHighestScoreSearchHandler(IRoomDirectoryService service) : IMessageHandler<RoomsWithHighestScoreSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomsWithHighestScoreSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.HighestScore, string.Empty, message.AdIndex);
        return true;
    }
}
