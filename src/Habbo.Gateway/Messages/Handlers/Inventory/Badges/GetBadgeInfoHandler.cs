using Habbo.Protocol.Messages.Incoming.Inventory.Badges;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>GetBadgeInfo — [badgeCode]: a badge display asks how many own its badge.</summary>
internal sealed class GetBadgeInfoHandler : IMessageHandler<GetBadgeInfoEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBadgeInfoEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendBadgeInfoAsync(message.BadgeCode);
        return true;
    }
}
