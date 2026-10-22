using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetRoomAdPurchaseInfoEvent — the rooms the player may promote.</summary>
internal sealed class GetRoomAdPurchaseInfoHandler(IRoomAdService service) : IMessageHandler<GetRoomAdPurchaseInfoEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRoomAdPurchaseInfoEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendPurchaseInfoAsync(player);
        return true;
    }
}
