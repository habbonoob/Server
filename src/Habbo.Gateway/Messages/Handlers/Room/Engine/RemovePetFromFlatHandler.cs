using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>RemovePetFromFlatMessageEvent — pick a pet up.</summary>
internal sealed class RemovePetFromFlatHandler : IMessageHandler<RemovePetFromFlatMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemovePetFromFlatMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RemovePetAsync(player.Id, message.PetId);
        return true;
    }
}
