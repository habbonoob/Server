using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetGuildEditInfoMessageEvent — a group as its manage window edits it.</summary>
internal sealed class GetGuildEditInfoHandler(IGroupService service) : IMessageHandler<GetGuildEditInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuildEditInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendEditInfoAsync(player.Id, message.GroupId);
        return true;
    }
}
