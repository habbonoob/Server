using Habbo.Protocol.Messages.Incoming.Recycler;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Recycler;

/// <summary>GetRecyclerPrizesMessageEvent — the Furni-Matic's prizes.</summary>
internal sealed class GetRecyclerPrizesHandler(IRecyclerService service) : IMessageHandler<GetRecyclerPrizesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRecyclerPrizesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendPrizesAsync(player);
        return true;
    }
}
