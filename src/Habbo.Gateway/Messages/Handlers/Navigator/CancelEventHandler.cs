using Habbo.Protocol.Messages.Incoming.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>CancelEventMessageEvent — end the room's promotion.</summary>
internal sealed class CancelEventHandler : IMessageHandler<CancelEventMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CancelEventMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CancelEventAsync(player, message.EventId);
        return true;
    }
}
