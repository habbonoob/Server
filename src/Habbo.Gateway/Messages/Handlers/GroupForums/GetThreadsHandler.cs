using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>GetThreadsMessageEvent — a page of a forum's threads.</summary>
internal sealed class GetThreadsHandler(IGroupForumService service) : IMessageHandler<GetThreadsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetThreadsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendThreadsAsync(player.Id, message.GroupId, message.StartIndex, message.Amount);
        return true;
    }
}
