using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>3152 SetRandomState — a double-click on a random-state item such as a fortune wheel. What happens is up to the item's behaviour.</summary>
internal sealed class SetRandomStateHandler : IMessageHandler<SetRandomStateMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, SetRandomStateMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.UseItemAsync(player.Id, message.ItemId, FurniAction.UseRandom, message.Param);
        return true;
    }
}
