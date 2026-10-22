using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>1875 BuildersClubPlaceRoomItem — Builders Club furni borrowed onto the floor of the room the player is
/// in. A placement that may not go ahead is answered with silence, as the warehouse already says why.</summary>
internal sealed class BuildersClubPlaceRoomItemHandler(IShopCatalog catalog)
    : IMessageHandler<BuildersClubPlaceRoomItemMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, BuildersClubPlaceRoomItemMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;
        if (context.Session.Room is not { } room
            || await BuildersClubPlacements.ForAsync(catalog, context, message.PageId, message.OfferId,
                message.ExtraParam, wall: false, message.Confirmed, ct) is not { } placement)
            return true;

        await room.PlaceBuildersClubFloorItemAsync(player.Id, placement, message.X, message.Y, message.Direction);
        return true;
    }
}
