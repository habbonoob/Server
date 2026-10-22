using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>RentableSpaceCancelRentMessageEvent — end a space's rent.</summary>
internal sealed class RentableSpaceCancelRentHandler : IMessageHandler<RentableSpaceCancelRentMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RentableSpaceCancelRentMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CancelRentAsync(player.Id, message.ItemId);
        return true;
    }
}
