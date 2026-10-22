using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Game;

/// <summary>Finds the SnowStorm game a player is in, or whose results they are looking at, and hands it a request.</summary>
internal static class SnowWarGames
{
    public static async Task ForPlayerAsync(IGrainFactory grains, Guid playerId, Func<ISnowWarGameGrain, Task> request)
    {
        if (await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).LastGameOfAsync(playerId) is { } gameId)
            await request(grains.GetGrain<ISnowWarGameGrain>(gameId));
    }
}
