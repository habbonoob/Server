using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>RemoveAdminRightsFromMemberMessageEvent — makes an admin a member again.</summary>
internal sealed class RemoveAdminRightsFromMemberHandler(IGroupService service) : IMessageHandler<RemoveAdminRightsFromMemberMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveAdminRightsFromMemberMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetAdminAsync(player.Id, message.GroupId, message.UserId, admin: false);
        return true;
    }
}
