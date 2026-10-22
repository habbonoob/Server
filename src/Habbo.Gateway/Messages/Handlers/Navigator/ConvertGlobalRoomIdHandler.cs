using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>ConvertGlobalRoomIdMessageEvent — the room a link from outside names.</summary>
internal sealed class ConvertGlobalRoomIdHandler(IRoomDirectoryService service) : IMessageHandler<ConvertGlobalRoomIdMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ConvertGlobalRoomIdMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ConvertGlobalRoomIdAsync(player, message.GlobalId);
        return true;
    }
}
