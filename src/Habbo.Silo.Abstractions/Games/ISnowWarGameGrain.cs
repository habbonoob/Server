using Orleans;

namespace Habbo.Silo.Abstractions.Games;

/// <summary>One SnowStorm game, from loading the arena to the results. Keyed by the game id.</summary>
public interface ISnowWarGameGrain : IGrainWithIntegerKey
{
    Task StartAsync(SnowWarGameSetup setup);

    Task LoadedAsync(Guid playerId, int percentage);

    /// <summary>The player walks towards a point, in game units.</summary>
    Task MoveAsync(Guid playerId, int x, int y);

    Task MakeSnowballAsync(Guid playerId);

    Task ThrowAtHumanAsync(Guid playerId, int targetId, int trajectory);

    Task ThrowAtPositionAsync(Guid playerId, int x, int y, int trajectory);

    /// <summary>The player's client lost step and wants the whole game again.</summary>
    Task RequestFullStatusAsync(Guid playerId);

    Task ChatAsync(Guid playerId, string message);

    /// <summary>The player leaves the arena.</summary>
    Task ExitAsync(Guid playerId);

    /// <summary>The player wants to play again after the results.</summary>
    Task PlayAgainAsync(Guid playerId);
}
