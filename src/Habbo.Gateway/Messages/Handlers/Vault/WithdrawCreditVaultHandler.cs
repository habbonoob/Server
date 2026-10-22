using Habbo.Protocol.Messages.Incoming.Vault;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Vault;

/// <summary>WithdrawCreditVaultMessageEvent — take the credits out of the vault.</summary>
internal sealed class WithdrawCreditVaultHandler(IEarningsService service) : IMessageHandler<WithdrawCreditVaultMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WithdrawCreditVaultMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.WithdrawVaultAsync(player);
        return true;
    }
}
