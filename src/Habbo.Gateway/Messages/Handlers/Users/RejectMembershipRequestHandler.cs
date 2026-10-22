using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>RejectMembershipRequestMessageEvent — turns an applicant away.</summary>
internal sealed class RejectMembershipRequestHandler(IGroupService service) : IMessageHandler<RejectMembershipRequestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RejectMembershipRequestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RejectRequestAsync(player.Id, message.GroupId, message.UserId);
        return true;
    }
}
