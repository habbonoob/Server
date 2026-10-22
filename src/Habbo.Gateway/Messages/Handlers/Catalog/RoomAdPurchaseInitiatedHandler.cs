using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>RoomAdPurchaseInitiatedEvent — the room ad page was opened.</summary>
internal sealed class RoomAdPurchaseInitiatedHandler(IRoomAdService service) : IMessageHandler<RoomAdPurchaseInitiatedEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomAdPurchaseInitiatedEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RecordPurchaseInitiatedAsync(player);
        return true;
    }
}
