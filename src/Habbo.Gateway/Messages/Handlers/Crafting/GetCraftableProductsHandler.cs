using Habbo.Protocol.Messages.Incoming.Crafting;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Crafting;

/// <summary>GetCraftableProductsEvent — an altar's recipes.</summary>
internal sealed class GetCraftableProductsHandler(ICraftingService service) : IMessageHandler<GetCraftableProductsEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCraftableProductsEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendCraftableProductsAsync(player, message.AltarId);
        return true;
    }
}
