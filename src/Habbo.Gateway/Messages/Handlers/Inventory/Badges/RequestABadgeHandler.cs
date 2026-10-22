using Habbo.Protocol.Messages.Incoming.Inventory.Badges;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>RequestABadgeEvent — Hands over the badge a request code stands for.</summary>
internal sealed class RequestABadgeHandler : IMessageHandler<RequestABadgeEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RequestABadgeEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.RequestBadgeAsync(message.RequestCode);
        return true;
    }
}
