using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetBonusRareInfoMessageEvent — the bonus rare.</summary>
internal sealed class GetBonusRareInfoHandler(ICatalogExtrasService service) : IMessageHandler<GetBonusRareInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBonusRareInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendBonusRareInfoAsync(player);
        return true;
    }
}
