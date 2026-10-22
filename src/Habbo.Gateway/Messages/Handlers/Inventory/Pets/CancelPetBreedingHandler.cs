using Habbo.Protocol.Messages.Incoming.Inventory.Pets;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Pets;

/// <summary>CancelPetBreedingEvent — send the pets in a nest away.</summary>
internal sealed class CancelPetBreedingHandler : IMessageHandler<CancelPetBreedingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CancelPetBreedingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CancelPetBreedingAsync(player.Id, message.NestId);
        return true;
    }
}
