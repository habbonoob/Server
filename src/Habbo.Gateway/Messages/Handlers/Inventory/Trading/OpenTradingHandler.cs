using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>OpenTradingEvent — start trading.</summary>
internal sealed class OpenTradingHandler : IMessageHandler<OpenTradingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenTradingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenTradingAsync(player.Id, message.RoomIndex);
        return true;
    }
}
