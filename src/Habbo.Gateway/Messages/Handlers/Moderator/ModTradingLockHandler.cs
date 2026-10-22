using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModTradingLockMessageEvent — lock a user out of trading.</summary>
internal sealed class ModTradingLockHandler(IModerationService service) : IMessageHandler<ModTradingLockMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModTradingLockMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SanctionAsync(player.Id, new ModerationAction(SanctionKind.TradeLock, message.UserId, message.Message, message.TopicId, message.IssueId, Minutes: message.Minutes));
        return true;
    }
}
