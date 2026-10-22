using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>RemoveItemMessageEvent — tear a note off the wall.</summary>
internal sealed class RemoveItemHandler : IMessageHandler<RemoveItemMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveItemMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RemovePostItAsync(player.Id, message.ItemId);
        return true;
    }
}
