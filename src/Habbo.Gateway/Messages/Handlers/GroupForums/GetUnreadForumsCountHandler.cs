using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>GetUnreadForumsCountMessageEvent — how many of the player's forums have something new.</summary>
internal sealed class GetUnreadForumsCountHandler(IGroupForumService service) : IMessageHandler<GetUnreadForumsCountMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetUnreadForumsCountMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendUnreadCountAsync(player.Id);
        return true;
    }
}
