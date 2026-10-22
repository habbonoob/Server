using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Trade;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeConfirmMessageEvent — accept or confirm a wired trade.</summary>
internal sealed class WiredTradeConfirmHandler : IMessageHandler<WiredTradeConfirmMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredTradeConfirmMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ConfirmWiredTradeAsync(player.Id, message.Confirmed);
        return true;
    }
}
