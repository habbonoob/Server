using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RoomsWhereMyFriendsAreSearchMessageEvent — the rooms friends are in.</summary>
internal sealed class RoomsWhereMyFriendsAreSearchHandler(IRoomDirectoryService service) : IMessageHandler<RoomsWhereMyFriendsAreSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomsWhereMyFriendsAreSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.RoomsWhereMyFriendsAre, string.Empty, null);
        return true;
    }
}
