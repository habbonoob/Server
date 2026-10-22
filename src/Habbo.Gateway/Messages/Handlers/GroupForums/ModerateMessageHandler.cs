using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>ModerateMessageMessageEvent — hides or restores a message.</summary>
internal sealed class ModerateMessageHandler(IGroupForumService service) : IMessageHandler<ModerateMessageMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModerateMessageMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ModerateMessageAsync(player.Id, message.GroupId, message.ThreadId, message.MessageId, message.State);
        return true;
    }
}
