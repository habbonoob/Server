using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Navigator;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>3512 DeleteFavouriteRoom — unstars a room.</summary>
internal sealed class DeleteFavouriteRoomHandler(IGrainFactory grains)
    : IMessageHandler<DeleteFavouriteRoomMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, DeleteFavouriteRoomMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        var navigator = grains.GetGrain<INavigatorGrain>(0);
        if (await navigator.SetFavouriteAsync(player.Id, message.RoomId, isFavourite: false))
            await context.SendAsync(new FavouriteChangedComposer(message.RoomId, IsFavourite: false), ct);

        return true;
    }
}
