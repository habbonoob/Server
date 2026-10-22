using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>ForwardToASubmittableRoomMessageEvent — to one of the player's rooms that could be entered.</summary>
internal sealed class ForwardToASubmittableRoomHandler(IRoomCompetitionService service) : IMessageHandler<ForwardToASubmittableRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ForwardToASubmittableRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ForwardToSubmittableRoomAsync(player);
        return true;
    }
}
