using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>PlacePostItMessageEvent — stick a note up from a pad.</summary>
internal sealed class PlacePostItHandler : IMessageHandler<PlacePostItMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PlacePostItMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.PlacePostItAsync(player.Id, message.ItemId, message.Location);
        return true;
    }
}
