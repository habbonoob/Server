using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>RentableSpaceStatusMessageEvent — a space's rent.</summary>
internal sealed class RentableSpaceStatusHandler : IMessageHandler<RentableSpaceStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RentableSpaceStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendRentableSpaceStatusAsync(player.Id, message.ItemId);
        return true;
    }
}
