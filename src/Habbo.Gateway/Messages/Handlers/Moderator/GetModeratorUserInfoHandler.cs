using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>GetModeratorUserInfoMessageEvent — a user for the user tool.</summary>
internal sealed class GetModeratorUserInfoHandler(IModerationService service) : IMessageHandler<GetModeratorUserInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetModeratorUserInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendUserInfoAsync(player.Id, message.UserId);
        return true;
    }
}
