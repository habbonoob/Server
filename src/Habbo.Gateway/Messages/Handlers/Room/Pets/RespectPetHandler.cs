using Habbo.Protocol.Messages.Incoming.Room.Pets;

namespace Habbo.Gateway.Messages.Handlers.Room.Pets;

/// <summary>RespectPetMessageEvent — respect a pet.</summary>
internal sealed class RespectPetHandler : IMessageHandler<RespectPetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RespectPetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RespectPetAsync(player.Id, message.PetId);
        return true;
    }
}
