using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>AddAdminRightsToMemberMessageEvent — makes a member an admin.</summary>
internal sealed class AddAdminRightsToMemberHandler(IGroupService service) : IMessageHandler<AddAdminRightsToMemberMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AddAdminRightsToMemberMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetAdminAsync(player.Id, message.GroupId, message.UserId, admin: true);
        return true;
    }
}
