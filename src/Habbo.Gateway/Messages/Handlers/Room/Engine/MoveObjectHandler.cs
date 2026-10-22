using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>3018 MoveObject — [itemId, x, y, dir] — also the rotate message (new dir, same tile).</summary>
internal sealed class MoveObjectHandler : IMessageHandler<MoveObjectMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, MoveObjectMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.MoveFloorItemAsync(player.Id, message.ItemId, message.X, message.Y, message.Rotation);
        return true;
    }
}
