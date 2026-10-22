using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>KickMemberMessageEvent — takes a member out, or leaves.</summary>
internal sealed class KickMemberHandler(IGroupService service) : IMessageHandler<KickMemberMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, KickMemberMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.KickAsync(player.Id, message.GroupId, message.UserId, message.Block);
        return true;
    }
}
