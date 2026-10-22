using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>UnacceptTradingEvent — take the acceptance back.</summary>
internal sealed class UnacceptTradingHandler : IMessageHandler<UnacceptTradingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnacceptTradingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetTradeAcceptedAsync(player.Id, accepted: false);
        return true;
    }
}
