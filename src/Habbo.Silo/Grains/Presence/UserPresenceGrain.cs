using Habbo.Silo.Abstractions.Players;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Presence;

[KeepAlive]
public sealed class UserPresenceGrain : Grain, IUserPresenceGrain
{
    private readonly HashSet<Guid> _online = [];

    public Task AddAsync(Guid playerId)
    {
        _online.Add(playerId);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid playerId)
    {
        _online.Remove(playerId);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync() => Task.FromResult(_online.Count);

    public Task<List<Guid>> GetOnlineAsync() => Task.FromResult(_online.ToList());

    public Task<List<Guid>> WhichAreOnlineAsync(List<Guid> playerIds) =>
        Task.FromResult(playerIds.Where(_online.Contains).ToList());
}
