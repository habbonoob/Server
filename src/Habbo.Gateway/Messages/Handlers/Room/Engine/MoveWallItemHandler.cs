using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>1614 MoveWallItem — [itemId, locationString] — also the wall rotate message.</summary>
internal sealed class MoveWallItemHandler : IMessageHandler<MoveWallItemMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, MoveWallItemMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.MoveWallItemAsync(player.Id, message.ItemId, message.Location);
        return true;
    }
}
