using Habbo.Protocol.Messages.Incoming.Room.Pets;

namespace Habbo.Gateway.Messages.Handlers.Room.Pets;

/// <summary>PetSelectedMessageEvent — a pet was picked out.</summary>
internal sealed class PetSelectedHandler : IMessageHandler<PetSelectedMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PetSelectedMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.PetSelectedAsync(player.Id, message.PetId);
        return true;
    }
}
