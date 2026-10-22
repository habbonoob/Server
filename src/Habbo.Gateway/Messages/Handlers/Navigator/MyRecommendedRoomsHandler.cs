using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyRecommendedRoomsMessageEvent — rooms like the ones the player visits.</summary>
internal sealed class MyRecommendedRoomsHandler(IRoomDirectoryService service) : IMessageHandler<MyRecommendedRoomsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyRecommendedRoomsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyRecommended, string.Empty, null);
        return true;
    }
}
