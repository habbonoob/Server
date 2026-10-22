using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>KickUserMessageEvent — send a user out of the room.</summary>
internal sealed class KickUserHandler : IMessageHandler<KickUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, KickUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.KickUserAsync(player.Id, message.UserId);
        return true;
    }
}
