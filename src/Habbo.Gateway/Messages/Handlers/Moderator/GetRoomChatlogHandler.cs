using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>GetRoomChatlogMessageEvent — what was said in a room lately.</summary>
internal sealed class GetRoomChatlogHandler(IModerationService service) : IMessageHandler<GetRoomChatlogMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRoomChatlogMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendRoomChatlogAsync(player.Id, message.RoomId);
        return true;
    }
}
