using Habbo.Protocol.Messages.Incoming.Crafting;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Crafting;

/// <summary>GetCraftingRecipeEvent — a recipe's ingredients.</summary>
internal sealed class GetCraftingRecipeHandler(ICraftingService service) : IMessageHandler<GetCraftingRecipeEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCraftingRecipeEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendRecipeAsync(player, message.ProductCode);
        return true;
    }
}
