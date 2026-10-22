using Habbo.Protocol.Messages.Incoming.Room.Pets;

namespace Habbo.Gateway.Messages.Handlers.Room.Pets;

/// <summary>BreedPetsMessageEvent — breed two plants.</summary>
internal sealed class BreedPetsHandler : IMessageHandler<BreedPetsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BreedPetsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.BreedPlantsAsync(player.Id, message.State, message.PetOneId, message.PetTwoId);
        return true;
    }
}
