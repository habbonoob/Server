using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetMemberGuildItemCountMessageEvent — a member's furni in the home room, before a kick.</summary>
internal sealed class GetMemberGuildItemCountHandler(IGroupService service) : IMessageHandler<GetMemberGuildItemCountMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMemberGuildItemCountMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendMemberFurniCountAsync(player.Id, message.GroupId, message.UserId);
        return true;
    }
}
