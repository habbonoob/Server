using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>744 LookTo.</summary>
internal sealed class LookToHandler : IMessageHandler<LookToMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, LookToMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.LookToAsync(player.Id, message.X, message.Y);
        return true;
    }
}
