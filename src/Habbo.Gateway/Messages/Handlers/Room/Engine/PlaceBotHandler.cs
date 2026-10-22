using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>PlaceBotMessageEvent — put a bot down.</summary>
internal sealed class PlaceBotHandler : IMessageHandler<PlaceBotMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PlaceBotMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.PlaceBotAsync(player.Id, message.BotId, message.X, message.Y);
        return true;
    }
}
