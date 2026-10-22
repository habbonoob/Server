using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>JoinHabboGroupMessageEvent — joins a group, or asks to.</summary>
internal sealed class JoinHabboGroupHandler(IGroupService service) : IMessageHandler<JoinHabboGroupMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, JoinHabboGroupMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.JoinAsync(player.Id, message.GroupId);
        return true;
    }
}
