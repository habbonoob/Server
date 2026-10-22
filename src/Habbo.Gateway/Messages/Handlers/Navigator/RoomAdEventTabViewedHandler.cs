using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RoomAdEventTabViewedEvent — the events tab was opened.</summary>
internal sealed class RoomAdEventTabViewedHandler(IRoomDirectoryService service) : IMessageHandler<RoomAdEventTabViewedEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomAdEventTabViewedEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RecordPromotionsTabViewedAsync(player);
        return true;
    }
}
