using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RoomAdSearchMessageEvent — the promoted rooms, top or newest.</summary>
internal sealed class RoomAdSearchHandler(IRoomDirectoryService service) : IMessageHandler<RoomAdSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomAdSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, (LegacyRoomSearch)message.SearchType, string.Empty, message.AdIndex);
        return true;
    }
}
