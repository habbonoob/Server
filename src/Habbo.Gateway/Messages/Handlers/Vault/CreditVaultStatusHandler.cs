using Habbo.Protocol.Messages.Incoming.Vault;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Vault;

/// <summary>CreditVaultStatusMessageEvent — the credit vault.</summary>
internal sealed class CreditVaultStatusHandler(IEarningsService service) : IMessageHandler<CreditVaultStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CreditVaultStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendVaultStatusAsync(player);
        return true;
    }
}
