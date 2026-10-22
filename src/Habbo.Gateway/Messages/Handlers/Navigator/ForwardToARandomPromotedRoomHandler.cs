using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>ForwardToARandomPromotedRoomMessageEvent — go to a room promoted in a category.</summary>
internal sealed class ForwardToARandomPromotedRoomHandler(IRoomDirectoryService service) : IMessageHandler<ForwardToARandomPromotedRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ForwardToARandomPromotedRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ForwardToRandomPromotedRoomAsync(player, message.CategoryCode);
        return true;
    }
}
