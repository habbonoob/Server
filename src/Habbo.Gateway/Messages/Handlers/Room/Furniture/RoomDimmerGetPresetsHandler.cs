using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>RoomDimmerGetPresetsMessageEvent — open the dimmer's dialog.</summary>
internal sealed class RoomDimmerGetPresetsHandler : IMessageHandler<RoomDimmerGetPresetsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomDimmerGetPresetsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendDimmerPresetsAsync(player.Id, message.ItemId);
        return true;
    }
}
