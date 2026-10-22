using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>3038 Dance.</summary>
internal sealed class DanceHandler : IMessageHandler<DanceMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, DanceMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.DanceAsync(player.Id, message.Style);
        return true;
    }
}
