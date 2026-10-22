using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>627 NewNavigatorInit — the navigator-ready gate. The contexts are the tabs the
/// client renders; "myworld_view" carries the player's own-rooms quick link.</summary>
internal sealed class NewNavigatorInitHandler(INavigatorCatalog catalog, IGrainFactory grains, INewNavigatorService newNavigator)
    : IMessageHandler<NewNavigatorInitEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, NewNavigatorInitEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        await context.SendAsync(NavigatorMetaDataComposer.From(await catalog.GetContextsAsync(ct)), ct);
        await newNavigator.SendPlayerStateAsync(player);

        var navigator = grains.GetGrain<INavigatorGrain>(0);
        await context.SendAsync(
            new FavouritesComposer(
                await navigator.GetFavouriteLimitAsync(),
                await navigator.GetFavouritesAsync(player.Id)),
            ct);

        return true;
    }
}
