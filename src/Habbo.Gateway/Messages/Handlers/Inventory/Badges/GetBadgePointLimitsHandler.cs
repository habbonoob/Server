using Habbo.Protocol.Messages.Incoming.Inventory.Badges;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>GetBadgePointLimitsEvent — The score each achievement badge level needs.</summary>
internal sealed class GetBadgePointLimitsHandler : IMessageHandler<GetBadgePointLimitsEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBadgePointLimitsEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendBadgePointLimitsAsync();
        return true;
    }
}
