using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>GiveSupplementToPetMessageEvent — water a plant or give it light.</summary>
internal sealed class GiveSupplementToPetHandler : IMessageHandler<GiveSupplementToPetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GiveSupplementToPetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.GiveSupplementAsync(player.Id, message.PetId, message.SupplementType);
        return true;
    }
}
