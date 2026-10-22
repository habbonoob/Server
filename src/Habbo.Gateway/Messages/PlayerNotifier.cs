using Habbo.Protocol.Composers;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages;

/// <summary>Pushes a packet at players wherever they are signed in, without needing to be in a room with them: through
/// each one's player grain, which knows their sessions on this gateway or another. Only those online are sent to, so
/// telling a whole friend list doesn't wake a grain for every friend who is away.</summary>
public sealed class PlayerNotifier(IGrainFactory grains)
{
    public Task SendAsync(Guid playerId, IComposer composer) => SendAsync([playerId], composer);

    public async Task SendAsync(IReadOnlyCollection<Guid> playerIds, IComposer composer)
    {
        if (playerIds.Count == 0)
            return;

        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key)
            .WhichAreOnlineAsync(playerIds.Distinct().ToList());
        if (online.Count == 0)
            return;

        var message = composer.ToMessage();
        await Task.WhenAll(online.Select(id => grains.GetGrain<IPlayerGrain>(id).SendAsync(message)));
    }
}
