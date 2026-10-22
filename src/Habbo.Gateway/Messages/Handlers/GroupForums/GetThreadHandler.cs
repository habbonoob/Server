using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>GetThreadMessageEvent — one thread.</summary>
internal sealed class GetThreadHandler(IGroupForumService service) : IMessageHandler<GetThreadMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetThreadMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendThreadAsync(player.Id, message.GroupId, message.ThreadId);
        return true;
    }
}
