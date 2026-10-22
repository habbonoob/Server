using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>MuteAllInRoomEvent — hush the room, or not.</summary>
internal sealed class MuteAllInRoomHandler : IMessageHandler<MuteAllInRoomEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MuteAllInRoomEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ToggleMuteAllAsync(player.Id);
        return true;
    }
}
