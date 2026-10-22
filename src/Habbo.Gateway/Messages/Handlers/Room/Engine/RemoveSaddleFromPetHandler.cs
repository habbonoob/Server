using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>RemoveSaddleFromPetMessageEvent — take a saddle off.</summary>
internal sealed class RemoveSaddleFromPetHandler : IMessageHandler<RemoveSaddleFromPetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveSaddleFromPetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RemoveSaddleAsync(player.Id, message.PetId);
        return true;
    }
}
