using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>TogglePetRidingPermissionMessageEvent — who may ride a pet.</summary>
internal sealed class TogglePetRidingPermissionHandler : IMessageHandler<TogglePetRidingPermissionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, TogglePetRidingPermissionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.TogglePetRidingAsync(player.Id, message.PetId);
        return true;
    }
}
