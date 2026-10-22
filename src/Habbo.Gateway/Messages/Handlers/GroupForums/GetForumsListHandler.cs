using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>GetForumsListMessageEvent — a list of forums.</summary>
internal sealed class GetForumsListHandler(IGroupForumService service) : IMessageHandler<GetForumsListMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetForumsListMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendForumsListAsync(player.Id, message.ListCode, message.StartIndex, message.Amount);
        return true;
    }
}
