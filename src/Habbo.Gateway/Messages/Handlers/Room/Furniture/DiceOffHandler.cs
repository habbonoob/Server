using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>1725 DiceOff — switch a dice off. What happens is up to the item's behaviour.</summary>
internal sealed class DiceOffHandler : IMessageHandler<DiceOffMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, DiceOffMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.UseItemAsync(player.Id, message.ItemId, FurniAction.DiceOff, 0);
        return true;
    }
}
