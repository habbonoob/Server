using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>CompostPlantMessageEvent — compost a dead plant.</summary>
internal sealed class CompostPlantHandler : IMessageHandler<CompostPlantMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CompostPlantMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CompostPlantAsync(player.Id, message.PetId);
        return true;
    }
}
