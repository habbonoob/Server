using Habbo.Protocol.Messages.Incoming.Inventory.Bots;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Bots;

/// <summary>GetBotInventoryEvent — the bots in the inventory.</summary>
internal sealed class GetBotInventoryHandler(IBotService service) : IMessageHandler<GetBotInventoryEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBotInventoryEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendInventoryAsync(player);
        return true;
    }
}
