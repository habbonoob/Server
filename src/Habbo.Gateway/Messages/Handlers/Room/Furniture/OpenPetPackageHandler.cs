using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>OpenPetPackageMessageEvent — name the pet in a box.</summary>
internal sealed class OpenPetPackageHandler : IMessageHandler<OpenPetPackageMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, OpenPetPackageMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.OpenPetPackageAsync(player.Id, message.ItemId, message.PetName);
        return true;
    }
}
