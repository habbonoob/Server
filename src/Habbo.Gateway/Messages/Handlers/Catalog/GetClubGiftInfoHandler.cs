using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetClubGiftInfoMessageEvent — the club's monthly gifts.</summary>
internal sealed class GetClubGiftInfoHandler(IClubService service) : IMessageHandler<GetClubGiftInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetClubGiftInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendGiftInfoAsync(player);
        return true;
    }
}
