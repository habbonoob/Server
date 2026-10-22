using Habbo.Protocol.Messages.Incoming.Room.Layout;

namespace Habbo.Gateway.Messages.Handlers.Room.Layout;

/// <summary>GetRoomEntryTileMessageEvent — where the room's door is.</summary>
internal sealed class GetRoomEntryTileHandler : IMessageHandler<GetRoomEntryTileMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRoomEntryTileMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendEntryTileAsync(player.Id);
        return true;
    }
}
