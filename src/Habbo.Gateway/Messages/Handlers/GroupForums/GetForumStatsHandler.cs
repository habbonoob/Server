using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>GetForumStatsMessageEvent — one forum and what the player may do in it.</summary>
internal sealed class GetForumStatsHandler(IGroupForumService service) : IMessageHandler<GetForumStatsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetForumStatsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendForumAsync(player.Id, message.GroupId);
        return true;
    }
}
