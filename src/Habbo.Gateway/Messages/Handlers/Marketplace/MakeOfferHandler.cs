using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>MakeOfferMessageEvent — put an item up for sale.</summary>
internal sealed class MakeOfferHandler(IMarketplaceService service) : IMessageHandler<MakeOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MakeOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.MakeOfferAsync(player, message.Price, message.FurniType,
            message.ItemIds ?? (message.ItemId is { } itemId ? [itemId] : []));
        return true;
    }
}
