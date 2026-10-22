using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetDirectClubBuyAvailableEvent — whether the club can be bought by text message.</summary>
internal sealed class GetDirectClubBuyAvailableHandler(IClubService service) : IMessageHandler<GetDirectClubBuyAvailableEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetDirectClubBuyAvailableEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendDirectBuyAvailabilityAsync(player, message.LengthInDays);
        return true;
    }
}
