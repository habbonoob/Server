using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>PurchaseTargetedOffer — buy a targeted offer at its own price: the refusal the client alerts
/// with, or the new balance.</summary>
internal sealed class PurchaseTargetedOfferHandler(ITargetedOfferService offers) : IMessageHandler<PurchaseTargetedOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseTargetedOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var result = await offers.PurchaseAsync(player, message.OfferId, message.Quantity);
        if (!result.Success)
        {
            await context.SendAsync(CatalogRefusals.For(result), ct);
            return true;
        }

        context.Session.RefreshCredits(result.RemainingCredits);
        await context.SendAsync(new CreditBalanceComposer(result.RemainingCredits), ct);
        return true;
    }
}
