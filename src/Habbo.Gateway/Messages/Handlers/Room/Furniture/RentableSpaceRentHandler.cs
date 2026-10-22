using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>RentableSpaceRentMessageEvent — rent a space.</summary>
internal sealed class RentableSpaceRentHandler : IMessageHandler<RentableSpaceRentMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RentableSpaceRentMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RentSpaceAsync(player.Id, message.ItemId);
        return true;
    }
}
