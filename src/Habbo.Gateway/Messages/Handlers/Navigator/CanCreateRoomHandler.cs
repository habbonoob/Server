using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>CanCreateRoomMessageEvent — whether the player may make another room.</summary>
internal sealed class CanCreateRoomHandler(IRoomDirectoryService service) : IMessageHandler<CanCreateRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CanCreateRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendCanCreateRoomAsync(player);
        return true;
    }
}
