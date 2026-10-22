using Habbo.Protocol.Messages.Incoming.GroupForums;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.GroupForums;

/// <summary>UpdateForumSettingsMessageEvent — who may read, post and moderate.</summary>
internal sealed class UpdateForumSettingsHandler(IGroupForumService service) : IMessageHandler<UpdateForumSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateForumSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateSettingsAsync(player.Id, message.GroupId, message.ReadPermission, message.PostMessagePermission, message.PostThreadPermission, message.ModeratePermission);
        return true;
    }
}
