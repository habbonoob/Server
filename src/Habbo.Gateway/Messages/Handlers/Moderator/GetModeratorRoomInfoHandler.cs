using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>GetModeratorRoomInfoMessageEvent — a room for the room tool.</summary>
internal sealed class GetModeratorRoomInfoHandler(IModerationService service) : IMessageHandler<GetModeratorRoomInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetModeratorRoomInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendRoomInfoAsync(player.Id, message.RoomId);
        return true;
    }
}
