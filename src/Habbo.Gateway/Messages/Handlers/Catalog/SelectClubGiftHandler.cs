using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>SelectClubGiftEvent — take a monthly gift.</summary>
internal sealed class SelectClubGiftHandler(IClubService service) : IMessageHandler<SelectClubGiftEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SelectClubGiftEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SelectGiftAsync(player, message.ProductCode);
        return true;
    }
}
