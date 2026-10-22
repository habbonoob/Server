using Habbo.Protocol.Messages.Incoming.Crafting;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Crafting;

/// <summary>GetCraftingRecipesAvailableEvent — secret recipes these items fit.</summary>
internal sealed class GetCraftingRecipesAvailableHandler(ICraftingService service) : IMessageHandler<GetCraftingRecipesAvailableEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCraftingRecipesAvailableEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendSecretRecipesAvailableAsync(player, message.AltarId, message.ItemIds);
        return true;
    }
}
