using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>AcceptTradingEvent — accept the trade.</summary>
internal sealed class AcceptTradingHandler : IMessageHandler<AcceptTradingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AcceptTradingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetTradeAcceptedAsync(player.Id, accepted: true);
        return true;
    }
}
