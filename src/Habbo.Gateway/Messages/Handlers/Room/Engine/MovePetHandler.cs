using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>MovePetMessageEvent — move a pet.</summary>
internal sealed class MovePetHandler : IMessageHandler<MovePetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MovePetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.MovePetAsync(player.Id, message.PetId, message.X, message.Y, message.Direction);
        return true;
    }
}
