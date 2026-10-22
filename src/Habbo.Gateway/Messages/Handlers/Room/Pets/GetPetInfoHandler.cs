using Habbo.Protocol.Messages.Incoming.Room.Pets;

namespace Habbo.Gateway.Messages.Handlers.Room.Pets;

/// <summary>GetPetInfoMessageEvent — a pet's details.</summary>
internal sealed class GetPetInfoHandler : IMessageHandler<GetPetInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetPetInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendPetInfoAsync(player.Id, message.PetId);
        return true;
    }
}
