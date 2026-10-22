using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredMenu;

/// <summary>WiredUpdateRoomEvent — reload or roll back the room.</summary>
internal sealed class WiredUpdateRoomHandler : IMessageHandler<WiredUpdateRoomEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredUpdateRoomEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.WiredUpdateRoomAsync(player.Id, message.RollBack);
        return true;
    }
}
