using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>ConfirmDeclineTradingEvent — decline at confirmation.</summary>
internal sealed class ConfirmDeclineTradingHandler : IMessageHandler<ConfirmDeclineTradingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ConfirmDeclineTradingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CloseTradeAsync(player.Id);
        return true;
    }
}
