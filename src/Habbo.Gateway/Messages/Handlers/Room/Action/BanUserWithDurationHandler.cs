using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>BanUserWithDurationMessageEvent — ban a user from the room.</summary>
internal sealed class BanUserWithDurationHandler : IMessageHandler<BanUserWithDurationMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BanUserWithDurationMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.BanUserAsync(player.Id, message.UserId, message.Duration);
        return true;
    }
}
