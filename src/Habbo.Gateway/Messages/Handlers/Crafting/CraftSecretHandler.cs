using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.Crafting;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Crafting;

/// <summary>CraftSecretEvent — craft a secret recipe.</summary>
internal sealed class CraftSecretHandler(ICraftingService service) : IMessageHandler<CraftSecretEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CraftSecretEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.CraftSecretAsync(player, message.AltarId, message.ItemIds.Select(InventoryStripId.ItemOf).ToList());
        return true;
    }
}
