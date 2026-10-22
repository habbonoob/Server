using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>RoomAdEventTabAdClickedEvent — a promoted room was clicked.</summary>
internal sealed class RoomAdEventTabAdClickedHandler(IRoomDirectoryService service) : IMessageHandler<RoomAdEventTabAdClickedEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomAdEventTabAdClickedEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RecordPromotionClickedAsync(player, message.FlatId);
        return true;
    }
}
