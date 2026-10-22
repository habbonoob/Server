using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>TogglePetBreedingPermissionMessageEvent — whether a pet may be bred.</summary>
internal sealed class TogglePetBreedingPermissionHandler : IMessageHandler<TogglePetBreedingPermissionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, TogglePetBreedingPermissionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.TogglePetBreedingAsync(player.Id, message.PetId);
        return true;
    }
}
