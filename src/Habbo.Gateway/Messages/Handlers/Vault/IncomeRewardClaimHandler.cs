using Habbo.Protocol.Messages.Incoming.Vault;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Vault;

/// <summary>IncomeRewardClaimMessageEvent — claim earnings.</summary>
internal sealed class IncomeRewardClaimHandler(IEarningsService service) : IMessageHandler<IncomeRewardClaimMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, IncomeRewardClaimMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ClaimAsync(player, message.Category);
        return true;
    }
}
