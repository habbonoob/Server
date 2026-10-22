using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UpdateGuildBadgeMessageEvent — redraws a group's badge.</summary>
internal sealed class UpdateGuildBadgeHandler(IGroupService service) : IMessageHandler<UpdateGuildBadgeMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateGuildBadgeMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateBadgeAsync(player.Id, message.GroupId, message.BadgeData);
        return true;
    }
}
