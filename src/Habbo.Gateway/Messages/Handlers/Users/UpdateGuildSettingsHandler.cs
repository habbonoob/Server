using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UpdateGuildSettingsMessageEvent — who may join, and whether members decorate.</summary>
internal sealed class UpdateGuildSettingsHandler(IGroupService service) : IMessageHandler<UpdateGuildSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateGuildSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateSettingsAsync(player.Id, message.GroupId, message.GuildType, message.RightsLevel);
        return true;
    }
}
