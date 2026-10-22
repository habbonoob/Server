using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>ApproveMembershipRequestMessageEvent — lets an applicant in.</summary>
internal sealed class ApproveMembershipRequestHandler(IGroupService service) : IMessageHandler<ApproveMembershipRequestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ApproveMembershipRequestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ApproveRequestAsync(player.Id, message.GroupId, message.UserId);
        return true;
    }
}
