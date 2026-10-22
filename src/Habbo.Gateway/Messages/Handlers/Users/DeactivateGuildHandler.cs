using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>DeactivateGuildMessageEvent — the owner deletes the group.</summary>
internal sealed class DeactivateGuildHandler(IGroupService service) : IMessageHandler<DeactivateGuildMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, DeactivateGuildMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.DeactivateAsync(player.Id, message.GroupId);
        return true;
    }
}
