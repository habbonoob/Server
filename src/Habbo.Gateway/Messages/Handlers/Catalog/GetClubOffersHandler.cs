using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetClubOffersMessageEvent — the club's offers.</summary>
internal sealed class GetClubOffersHandler(IClubService service) : IMessageHandler<GetClubOffersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetClubOffersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendOffersAsync(player, message.Source);
        return true;
    }
}
