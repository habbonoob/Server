using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>BuyHabbiconMessageEvent — buy a Habbicon; the hub waits for PurchaseOK or a refusal.</summary>
internal sealed class BuyHabbiconHandler(IHabbiconService habbicons) : IMessageHandler<BuyHabbiconMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BuyHabbiconMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.BuyAsync(player, message.HabbiconId);
        return true;
    }
}
