using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>2125 PickupObject — [typeCode(2 floor/1 wall), roomItemId, confirmed]. The type code
/// goes unread: the grain knows the type from the item itself.</summary>
internal sealed class PickupObjectHandler : IMessageHandler<PickupObjectMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, PickupObjectMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.PickupItemAsync(player.Id, message.ItemId, message.Confirmed);
        return true;
    }
}
