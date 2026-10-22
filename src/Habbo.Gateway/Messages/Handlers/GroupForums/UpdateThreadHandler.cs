using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>UpdateThreadMessageEvent — pins or locks a thread.</summary>
internal sealed class UpdateThreadHandler(IGroupForumService service) : IMessageHandler<UpdateThreadMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateThreadMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateThreadAsync(player.Id, message.GroupId, message.ThreadId, message.IsSticky, message.IsLocked);
        return true;
    }
}
