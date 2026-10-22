using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>PostMessageMessageEvent — starts a thread or replies.</summary>
internal sealed class PostMessageHandler(IGroupForumService service) : IMessageHandler<PostMessageMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PostMessageMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.PostAsync(player.Id, message.GroupId, message.ThreadId, message.Subject, message.Message);
        return true;
    }
}
