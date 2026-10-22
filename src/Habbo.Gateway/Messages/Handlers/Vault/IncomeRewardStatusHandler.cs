using Habbo.Protocol.Messages.Incoming.Vault;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Vault;

/// <summary>IncomeRewardStatusMessageEvent — earnings waiting to be claimed.</summary>
internal sealed class IncomeRewardStatusHandler(IEarningsService service) : IMessageHandler<IncomeRewardStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, IncomeRewardStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendStatusAsync(player);
        return true;
    }
}
