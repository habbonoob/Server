using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>MakeNftMarketplaceOfferMessageEvent — put an NFT up for sale for emeralds.</summary>
internal sealed class MakeNftMarketplaceOfferHandler(IMarketplaceService service) : IMessageHandler<MakeNftMarketplaceOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MakeNftMarketplaceOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.MakeNftOfferAsync(player, message.NftFurniType, message.ItemId, message.Price);
        return true;
    }
}
