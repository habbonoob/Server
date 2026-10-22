using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UpdateGuildColorsMessageEvent — recolours a group.</summary>
internal sealed class UpdateGuildColorsHandler(IGroupService service) : IMessageHandler<UpdateGuildColorsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateGuildColorsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateColorsAsync(player.Id, message.GroupId, message.PrimaryColorId, message.SecondaryColorId);
        return true;
    }
}
