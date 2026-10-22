using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>HarvestPetMessageEvent — harvest a grown plant.</summary>
internal sealed class HarvestPetHandler : IMessageHandler<HarvestPetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, HarvestPetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.HarvestPlantAsync(player.Id, message.PetId);
        return true;
    }
}
