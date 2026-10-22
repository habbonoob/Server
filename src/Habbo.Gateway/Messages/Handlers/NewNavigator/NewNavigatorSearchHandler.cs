using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.NewNavigator;
using Habbo.Protocol.Messages.Outgoing.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.NewNavigator;

/// <summary>815 NewNavigatorSearch — a tab, quick link or typed filter; the search grain
/// expands the code into result blocks.</summary>
internal sealed class NewNavigatorSearchHandler(IGrainFactory grains)
    : IMessageHandler<NewNavigatorSearchEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, NewNavigatorSearchEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        var blocks = await grains.GetGrain<INavigatorSearchGrain>(0)
            .SearchAsync(player.Id, message.SearchCode, message.Filter);
        await context.SendAsync(
            NavigatorSearchResultBlocksComposer.From(message.SearchCode, message.Filter, blocks), ct);
        return true;
    }
}
