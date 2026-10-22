using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyRoomRightsSearchMessageEvent — the rooms the player has rights in.</summary>
internal sealed class MyRoomRightsSearchHandler(IRoomDirectoryService service) : IMessageHandler<MyRoomRightsSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyRoomRightsSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyRoomRights, string.Empty, null);
        return true;
    }
}
