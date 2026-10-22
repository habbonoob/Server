using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>GetRoomVisitsMessageEvent — the rooms a user was in.</summary>
internal sealed class GetRoomVisitsHandler(IModerationService service) : IMessageHandler<GetRoomVisitsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetRoomVisitsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendRoomVisitsAsync(player.Id, message.UserId);
        return true;
    }
}
