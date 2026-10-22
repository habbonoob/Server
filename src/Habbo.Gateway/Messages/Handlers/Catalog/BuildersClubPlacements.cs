using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>A Builders Club offer the client dragged into a room, made into what the room needs: the one piece of
/// furniture it lends. Null for an offer not in the warehouse, one that is
/// not a single piece of furniture of that kind (the client offers no other to drag), or one with none to lend.</summary>
internal static class BuildersClubPlacements
{
    public static async Task<BuildersClubPlacement?> ForAsync(
        IShopCatalog catalog, GatewaySessionContext context,
        int pageId, int offerId, string extraParam, bool wall, bool confirmed, CancellationToken ct)
    {
        var player = context.Session.Player!;
        if (await catalog.GetBuildersClubOfferAsync(pageId, offerId, player.Rank, ct) is not { Products: [{ DefinitionId: { } definitionId } product] }
            || product.Type != ProductTypes.Furniture || product.IsWallItem != wall)
            return null;

        return new BuildersClubPlacement(pageId, offerId, extraParam, definitionId, product.ExtraParam, confirmed);
    }
}
