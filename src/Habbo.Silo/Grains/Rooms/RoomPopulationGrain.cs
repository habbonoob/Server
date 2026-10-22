using Habbo.Silo.Abstractions.Navigator;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Live user counts per room, reported by the room grains as avatars come and go, with the silo each room is
/// held on: when one goes, the rooms held there are empty. Kept alive so counts survive idle periods; empty rooms drop
/// out of the map.</summary>
[KeepAlive]
public sealed class RoomPopulationGrain : Grain, IRoomPopulationGrain
{
    private readonly Dictionary<int, (int Count, SiloAddress Host)> _population = [];

    public Task SetAsync(int roomId, int userCount, SiloAddress host)
    {
        if (userCount > 0)
            _population[roomId] = (userCount, host);
        else
            _population.Remove(roomId);

        return Task.CompletedTask;
    }

    public Task ForgetHostAsync(SiloAddress host)
    {
        foreach (var roomId in _population.Where(room => room.Value.Host.Equals(host)).Select(room => room.Key).ToList())
            _population.Remove(roomId);
        return Task.CompletedTask;
    }

    public Task<Dictionary<int, int>> GetAsync() =>
        Task.FromResult(_population.ToDictionary(room => room.Key, room => room.Value.Count));
}
