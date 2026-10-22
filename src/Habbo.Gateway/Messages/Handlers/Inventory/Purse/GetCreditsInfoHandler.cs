using Habbo.Protocol.Messages.Incoming.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Purse;

/// <summary>3719 GetCreditsInfo — answer the purse with the player's credit balance.</summary>
internal sealed class GetCreditsInfoHandler : IMessageHandler<GetCreditsInfoEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetCreditsInfoEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(new CreditBalanceComposer(player.Credits), ct);
        return true;
    }
}
