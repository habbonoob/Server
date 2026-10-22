using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyFavouriteRoomsSearchMessageEvent — the rooms the player starred.</summary>
internal sealed class MyFavouriteRoomsSearchHandler(IRoomDirectoryService service) : IMessageHandler<MyFavouriteRoomsSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyFavouriteRoomsSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyFavourites, string.Empty, null);
        return true;
    }
}
