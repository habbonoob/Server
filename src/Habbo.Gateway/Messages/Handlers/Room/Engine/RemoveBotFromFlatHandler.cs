using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>RemoveBotFromFlatMessageEvent — pick a bot up.</summary>
internal sealed class RemoveBotFromFlatHandler : IMessageHandler<RemoveBotFromFlatMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveBotFromFlatMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RemoveBotAsync(player.Id, message.BotId);
        return true;
    }
}
