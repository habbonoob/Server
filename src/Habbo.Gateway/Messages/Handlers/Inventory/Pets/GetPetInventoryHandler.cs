using Habbo.Protocol.Messages.Incoming.Inventory.Pets;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Pets;

/// <summary>GetPetInventoryEvent — the pets in the inventory.</summary>
internal sealed class GetPetInventoryHandler(IPetService service) : IMessageHandler<GetPetInventoryEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetPetInventoryEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendInventoryAsync(player);
        return true;
    }
}
