using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>RedeemVoucherMessageEvent — redeem a voucher code.</summary>
internal sealed class RedeemVoucherHandler(IVoucherService service) : IMessageHandler<RedeemVoucherMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RedeemVoucherMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RedeemAsync(player, message.Code);
        return true;
    }
}
