using Habbo.Protocol.Messages.Incoming.RoomDirectory;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.RoomDirectory;

/// <summary>RoomNetworkOpenConnectionMessageEvent — enter a room network.</summary>
internal sealed class RoomNetworkOpenConnectionHandler(IRoomDirectoryService service) : IMessageHandler<RoomNetworkOpenConnectionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomNetworkOpenConnectionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.OpenRoomNetworkAsync(player, message.NetworkId, message.HomeRoomId);
        return true;
    }
}
