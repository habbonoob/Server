using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>1461 CustomizeAvatarWithFurni — use a clothing furni: its clothing is the player's from now on.</summary>
internal sealed class CustomizeAvatarWithFurniHandler : IMessageHandler<CustomizeAvatarWithFurniMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CustomizeAvatarWithFurniMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.BindClothingAsync(player.Id, message.ItemId);
        return true;
    }
}
