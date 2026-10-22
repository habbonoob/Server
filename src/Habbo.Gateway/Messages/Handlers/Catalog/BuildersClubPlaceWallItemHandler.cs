using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>313 BuildersClubPlaceWallItem — Builders Club furni borrowed onto a wall of the room the player is in.</summary>
internal sealed class BuildersClubPlaceWallItemHandler(IShopCatalog catalog)
    : IMessageHandler<BuildersClubPlaceWallItemMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, BuildersClubPlaceWallItemMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;
        if (context.Session.Room is not { } room
            || await BuildersClubPlacements.ForAsync(catalog, context, message.PageId, message.OfferId,
                message.ExtraParam, wall: true, message.Confirmed, ct) is not { } placement)
            return true;

        await room.PlaceBuildersClubWallItemAsync(player.Id, placement, message.WallLocation);
        return true;
    }
}
