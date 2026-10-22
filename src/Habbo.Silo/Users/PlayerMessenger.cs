using Habbo.Protocol.Composers;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Silo.Users;

/// <summary>Sends a packet to a player wherever they are connected, for services that answer
/// players outside of a room.</summary>
public sealed class PlayerMessenger(IGrainFactory grains)
{
    public Task SendAsync(Guid playerId, IComposer composer) =>
        grains.GetGrain<IPlayerGrain>(playerId).SendAsync(composer.ToMessage());

    public Task SendAsync(IEnumerable<Guid> playerIds, IComposer composer)
    {
        var message = composer.ToMessage();
        return Task.WhenAll(playerIds.Distinct().Select(id => grains.GetGrain<IPlayerGrain>(id).SendAsync(message)));
    }
}
