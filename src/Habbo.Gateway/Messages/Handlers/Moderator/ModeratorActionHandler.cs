using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModeratorActionMessageEvent — message (and perhaps kick) everyone in the room.</summary>
internal sealed class ModeratorActionHandler(IModerationService service) : IMessageHandler<ModeratorActionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModeratorActionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RoomActionAsync(player.Id, message.ActionType, message.Message);
        return true;
    }
}
