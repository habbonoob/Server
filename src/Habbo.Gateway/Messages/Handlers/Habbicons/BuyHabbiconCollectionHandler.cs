using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>BuyHabbiconCollectionMessageEvent — buy what the player lacks of a set, for the set's price.</summary>
internal sealed class BuyHabbiconCollectionHandler(IHabbiconService habbicons) : IMessageHandler<BuyHabbiconCollectionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BuyHabbiconCollectionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.BuyCollectionAsync(player, message.CollectionId);
        return true;
    }
}
