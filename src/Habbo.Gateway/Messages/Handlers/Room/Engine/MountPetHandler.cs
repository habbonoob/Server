using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>MountPetMessageEvent — get on or off a pet.</summary>
internal sealed class MountPetHandler : IMessageHandler<MountPetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MountPetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.MountPetAsync(player.Id, message.PetId, message.Mount);
        return true;
    }
}
