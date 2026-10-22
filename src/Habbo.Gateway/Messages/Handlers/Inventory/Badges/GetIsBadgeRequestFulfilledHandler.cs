using Habbo.Protocol.Messages.Incoming.Inventory.Badges;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>GetIsBadgeRequestFulfilledEvent — Whether the player has what a request code gives.</summary>
internal sealed class GetIsBadgeRequestFulfilledHandler : IMessageHandler<GetIsBadgeRequestFulfilledEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetIsBadgeRequestFulfilledEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendBadgeRequestStatusAsync(message.RequestCode);
        return true;
    }
}
