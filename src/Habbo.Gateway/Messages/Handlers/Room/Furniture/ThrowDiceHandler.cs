using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>339 ThrowDice — throw a dice. What happens is up to the item's behaviour.</summary>
internal sealed class ThrowDiceHandler : IMessageHandler<ThrowDiceMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, ThrowDiceMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.UseItemAsync(player.Id, message.ItemId, FurniAction.ThrowDice, 0);
        return true;
    }
}
