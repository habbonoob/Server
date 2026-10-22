using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>PlacePetMessageEvent — put a pet down.</summary>
internal sealed class PlacePetHandler : IMessageHandler<PlacePetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PlacePetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.PlacePetAsync(player.Id, message.PetId, message.X, message.Y);
        return true;
    }
}
