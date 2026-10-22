using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>GetMessagesMessageEvent — a page of a thread's messages.</summary>
internal sealed class GetMessagesHandler(IGroupForumService service) : IMessageHandler<GetMessagesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMessagesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendMessagesAsync(player.Id, message.GroupId, message.ThreadId, message.StartIndex, message.Amount);
        return true;
    }
}
