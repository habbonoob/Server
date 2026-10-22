using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Engine;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>3034 UseWallItem — UseFurniture for a wall item. What happens is up to the item's behaviour.</summary>
internal sealed class UseWallItemHandler : IMessageHandler<UseWallItemMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, UseWallItemMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.UseItemAsync(player.Id, message.ItemId, FurniAction.Use, message.Param);
        return true;
    }
}
