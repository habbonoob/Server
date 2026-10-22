using Habbo.Protocol.Messages.Incoming.Inventory.Pets;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Pets;

/// <summary>ConfirmPetBreedingEvent — breed the pets in a nest.</summary>
internal sealed class ConfirmPetBreedingHandler : IMessageHandler<ConfirmPetBreedingEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ConfirmPetBreedingEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ConfirmPetBreedingAsync(player.Id, message.NestId, message.Name, message.PetOneId, message.PetTwoId);
        return true;
    }
}
