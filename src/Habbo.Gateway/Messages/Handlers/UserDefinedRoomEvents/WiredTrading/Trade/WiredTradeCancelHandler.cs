using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Trade;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeCancelMessageEvent — call off a wired trade.</summary>
internal sealed class WiredTradeCancelHandler : IMessageHandler<WiredTradeCancelMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredTradeCancelMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CancelWiredTradeAsync(player.Id);
        return true;
    }
}
