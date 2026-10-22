using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>ConfirmAcceptTradingEvent — confirm the trade.</summary>
internal sealed class ConfirmAcceptTradingHandler : IMessageHandler<ConfirmAcceptTradingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ConfirmAcceptTradingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ConfirmTradeAsync(player.Id);
        return true;
    }
}
