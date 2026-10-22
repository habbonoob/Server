using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UpdateGuildIdentityMessageEvent — renames or redescribes a group.</summary>
internal sealed class UpdateGuildIdentityHandler(IGroupService service) : IMessageHandler<UpdateGuildIdentityMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateGuildIdentityMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateIdentityAsync(player.Id, message.GroupId, message.Name, message.Description);
        return true;
    }
}
