using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>RoomDimmerChangeStateMessageEvent — switch the dimmer on or off.</summary>
internal sealed class RoomDimmerChangeStateHandler : IMessageHandler<RoomDimmerChangeStateMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomDimmerChangeStateMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ToggleDimmerAsync(player.Id, message.ItemId);
        return true;
    }
}
