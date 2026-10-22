using Habbo.Protocol.Messages.Incoming.Room.Session;

namespace Habbo.Gateway.Messages.Handlers.Room.Session;

/// <summary>ChangeQueueMessageEvent — move to the spectators' line or the visitors'.</summary>
internal sealed class ChangeQueueHandler : IMessageHandler<ChangeQueueMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChangeQueueMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ChangeQueueAsync(player.Id, player, message.Target);
        return true;
    }
}
