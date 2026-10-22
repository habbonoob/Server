using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Navigator;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>3089 AddFavouriteRoom — stars a room. The client only redraws the star once we
/// confirm, so a refused star (unknown room, or the limit reached) simply stays off.</summary>
internal sealed class AddFavouriteRoomHandler(IGrainFactory grains)
    : IMessageHandler<AddFavouriteRoomMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, AddFavouriteRoomMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        var navigator = grains.GetGrain<INavigatorGrain>(0);
        if (await navigator.SetFavouriteAsync(player.Id, message.RoomId, isFavourite: true))
            await context.SendAsync(new FavouriteChangedComposer(message.RoomId, IsFavourite: true), ct);

        return true;
    }
}
