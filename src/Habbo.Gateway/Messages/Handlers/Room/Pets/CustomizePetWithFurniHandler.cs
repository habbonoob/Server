using Habbo.Protocol.Messages.Incoming.Room.Pets;

namespace Habbo.Gateway.Messages.Handlers.Room.Pets;

/// <summary>CustomizePetWithFurniEvent — use a pet product.</summary>
internal sealed class CustomizePetWithFurniHandler : IMessageHandler<CustomizePetWithFurniEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CustomizePetWithFurniEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CustomizePetAsync(player.Id, message.ItemId, message.PetId);
        return true;
    }
}
