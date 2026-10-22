using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Engine;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>2183 UseFurniture — a double-click on a floor item, or the infostand's Use button. What happens is up to the item's behaviour.</summary>
internal sealed class UseFurnitureHandler : IMessageHandler<UseFurnitureMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, UseFurnitureMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.UseItemAsync(player.Id, message.ItemId, FurniAction.Use, message.Param);
        return true;
    }
}
