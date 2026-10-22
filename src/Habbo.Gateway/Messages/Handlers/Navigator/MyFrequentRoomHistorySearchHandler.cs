using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyFrequentRoomHistorySearchMessageEvent — the rooms the player visits most.</summary>
internal sealed class MyFrequentRoomHistorySearchHandler(IRoomDirectoryService service) : IMessageHandler<MyFrequentRoomHistorySearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyFrequentRoomHistorySearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyFrequentHistory, string.Empty, null);
        return true;
    }
}
