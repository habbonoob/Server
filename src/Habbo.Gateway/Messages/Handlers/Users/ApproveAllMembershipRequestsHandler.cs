using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>ApproveAllMembershipRequestsMessageEvent — lets every applicant in.</summary>
internal sealed class ApproveAllMembershipRequestsHandler(IGroupService service) : IMessageHandler<ApproveAllMembershipRequestsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ApproveAllMembershipRequestsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ApproveAllRequestsAsync(player.Id, message.GroupId);
        return true;
    }
}
