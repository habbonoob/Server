using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RoomTextSearchMessageEvent — the search box, with its tag:, owner:, group: and roomname: prefixes.</summary>
internal sealed class RoomTextSearchHandler(IRoomDirectoryService service) : IMessageHandler<RoomTextSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomTextSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.Text, message.SearchText, null);
        return true;
    }
}
