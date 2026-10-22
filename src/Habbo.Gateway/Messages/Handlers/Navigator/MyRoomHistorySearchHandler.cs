using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyRoomHistorySearchMessageEvent — the rooms the player was in lately.</summary>
internal sealed class MyRoomHistorySearchHandler(IRoomDirectoryService service) : IMessageHandler<MyRoomHistorySearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyRoomHistorySearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyHistory, string.Empty, null);
        return true;
    }
}
