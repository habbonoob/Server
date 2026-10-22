using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetGuildMembershipsMessageEvent — the player's groups.</summary>
internal sealed class GetGuildMembershipsHandler(IGroupService service) : IMessageHandler<GetGuildMembershipsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuildMembershipsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendMembershipsAsync(player.Id);
        return true;
    }
}
