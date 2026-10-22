using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>DropCarryItemMessageEvent — Puts down what the avatar carries.</summary>
internal sealed class DropCarryItemHandler : IMessageHandler<DropCarryItemMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, DropCarryItemMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.DropHandItemAsync(player.Id);
        return true;
    }
}
