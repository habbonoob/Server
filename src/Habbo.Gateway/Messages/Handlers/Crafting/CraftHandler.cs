using Habbo.Protocol.Messages.Incoming.Crafting;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Crafting;

/// <summary>CraftEvent — craft a recipe.</summary>
internal sealed class CraftHandler(ICraftingService service) : IMessageHandler<CraftEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CraftEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.CraftAsync(player, message.AltarId, message.RecipeCode);
        return true;
    }
}
