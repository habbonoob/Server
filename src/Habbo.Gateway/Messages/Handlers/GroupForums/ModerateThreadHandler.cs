using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>ModerateThreadMessageEvent — hides or restores a thread.</summary>
internal sealed class ModerateThreadHandler(IGroupForumService service) : IMessageHandler<ModerateThreadMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModerateThreadMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ModerateThreadAsync(player.Id, message.GroupId, message.ThreadId, message.State);
        return true;
    }
}
