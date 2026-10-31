using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>GetHabbiconShopDataMessageEvent — the collections, as the player stands with them.</summary>
internal sealed class GetHabbiconShopDataHandler(IHabbiconService habbicons) : IMessageHandler<GetHabbiconShopDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHabbiconShopDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.SendShopDataAsync(player);
        return true;
    }
}
