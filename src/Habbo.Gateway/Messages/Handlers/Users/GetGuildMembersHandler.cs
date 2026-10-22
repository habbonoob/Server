using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetGuildMembersMessageEvent — a page of the member list.</summary>
internal sealed class GetGuildMembersHandler(IGroupService service) : IMessageHandler<GetGuildMembersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuildMembersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendMembersAsync(player.Id, message.GroupId, message.PageIndex, message.UserNameFilter, message.SearchType);
        return true;
    }
}
