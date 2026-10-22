using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>ForwardToSomeRoomMessageEvent — go to a named room.</summary>
internal sealed class ForwardToSomeRoomHandler(IRoomDirectoryService service) : IMessageHandler<ForwardToSomeRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ForwardToSomeRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ForwardToSomeRoomAsync(player, message.Type);
        return true;
    }
}
