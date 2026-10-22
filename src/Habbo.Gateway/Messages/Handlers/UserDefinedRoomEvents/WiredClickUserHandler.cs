using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>2992 WiredClickUser.</summary>
internal sealed class WiredClickUserHandler : IMessageHandler<WiredClickUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredClickUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ClickUserAsync(player.Id, message.RoomIndex);
        return true;
    }
}
