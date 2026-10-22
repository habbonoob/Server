using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Navigator;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>893 GetPopularRoomTags — the navigator's tag cloud, counted from the rooms themselves.</summary>
internal sealed class GetPopularRoomTagsHandler(IGrainFactory grains)
    : IMessageHandler<GetPopularRoomTagsMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetPopularRoomTagsMessageEvent message, CancellationToken ct)
    {
        var tags = await grains.GetGrain<INavigatorGrain>(0).GetPopularTagsAsync();
        await context.SendAsync(PopularRoomTagsResultComposer.From(tags), ct);
        return true;
    }
}
