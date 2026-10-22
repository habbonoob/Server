using Habbo.Protocol.Messages.Incoming.Recycler;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Recycler;

/// <summary>GetRecyclerStatusMessageEvent — whether the Furni-Matic is ready.</summary>
internal sealed class GetRecyclerStatusHandler(IRecyclerService service) : IMessageHandler<GetRecyclerStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRecyclerStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendStatusAsync(player);
        return true;
    }
}
