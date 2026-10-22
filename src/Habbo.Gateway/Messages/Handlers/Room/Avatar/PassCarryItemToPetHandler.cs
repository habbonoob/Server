using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>PassCarryItemToPetMessageEvent — hand a pet what the player carries.</summary>
internal sealed class PassCarryItemToPetHandler : IMessageHandler<PassCarryItemToPetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PassCarryItemToPetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.PassHandItemToPetAsync(player.Id, message.PetId);
        return true;
    }
}
