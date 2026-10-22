using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UnblockGroupMemberMessageEvent — lets a blocked user back in.</summary>
internal sealed class UnblockGroupMemberHandler(IGroupService service) : IMessageHandler<UnblockGroupMemberMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnblockGroupMemberMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UnblockAsync(player.Id, message.GroupId, message.UserId);
        return true;
    }
}
