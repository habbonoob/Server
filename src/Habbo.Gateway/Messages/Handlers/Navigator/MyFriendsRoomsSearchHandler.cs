using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyFriendsRoomsSearchMessageEvent — the rooms friends own.</summary>
internal sealed class MyFriendsRoomsSearchHandler(IRoomDirectoryService service) : IMessageHandler<MyFriendsRoomsSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyFriendsRoomsSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyFriendsRooms, string.Empty, null);
        return true;
    }
}
