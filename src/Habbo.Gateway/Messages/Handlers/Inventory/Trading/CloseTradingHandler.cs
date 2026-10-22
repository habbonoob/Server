using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>CloseTradingEvent — close the trade.</summary>
internal sealed class CloseTradingHandler : IMessageHandler<CloseTradingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CloseTradingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CloseTradeAsync(player.Id);
        return true;
    }
}
