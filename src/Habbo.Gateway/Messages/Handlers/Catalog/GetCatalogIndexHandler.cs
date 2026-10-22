using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>2933 GetCatalogIndex — the page tree. The client keeps its catalog closed until
/// this arrives, so an empty tree is still an answer.</summary>
internal sealed class GetCatalogIndexHandler(IShopCatalog catalog)
    : IMessageHandler<GetCatalogIndexEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetCatalogIndexEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        // the client asks in the mode of the catalog it has open: the shop (NORMAL) or the Builders Club warehouse
        var buildersClub = message.CatalogType == CatalogModes.BuildersClub;
        var tree = await catalog.GetTreeAsync(player.Rank, ct, buildersClub);
        await context.SendAsync(
            new CatalogIndexMessageComposer(
                CatalogNodeFactory.From(tree),
                NewAdditionsAvailable: false,
                message.CatalogType),
            ct);

        return true;
    }
}
