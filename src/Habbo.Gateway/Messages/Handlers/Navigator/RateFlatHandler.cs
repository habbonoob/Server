using Habbo.Protocol.Messages.Incoming.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RateFlatMessageEvent — like the room.</summary>
internal sealed class RateFlatHandler : IMessageHandler<RateFlatMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RateFlatMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RateAsync(player.Id, message.Points);
        return true;
    }
}
