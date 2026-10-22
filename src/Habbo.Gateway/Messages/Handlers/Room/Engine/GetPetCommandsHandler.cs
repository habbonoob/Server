using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>GetPetCommandsMessageEvent — the commands a pet knows.</summary>
internal sealed class GetPetCommandsHandler : IMessageHandler<GetPetCommandsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetPetCommandsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendPetCommandsAsync(player.Id, message.PetId);
        return true;
    }
}
