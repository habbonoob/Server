using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyRoomsSearchMessageEvent — the player's own rooms.</summary>
internal sealed class MyRoomsSearchHandler(IRoomDirectoryService service) : IMessageHandler<MyRoomsSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyRoomsSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyRooms, string.Empty, null);
        return true;
    }
}
