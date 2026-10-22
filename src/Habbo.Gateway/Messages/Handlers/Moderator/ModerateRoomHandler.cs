using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModerateRoomMessageEvent — take a room in hand.</summary>
internal sealed class ModerateRoomHandler(IModerationService service) : IMessageHandler<ModerateRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModerateRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ModerateRoomAsync(player.Id, message.RoomId, message.LockDoor != 0, message.ChangeTitle != 0, message.KickUsers != 0);
        return true;
    }
}
