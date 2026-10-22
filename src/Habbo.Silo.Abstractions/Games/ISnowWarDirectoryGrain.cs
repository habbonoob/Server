using Orleans;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Games;

/// <summary>The game center's SnowStorm directory: the games waiting for players, who waits in which, and which game
/// each player is in. One for the hotel (key 0).</summary>
public interface ISnowWarDirectoryGrain : IGrainWithIntegerKey
{
    const int Key = 0;

    /// <summary>Tells the player whether the directory is open, and how many games they have played and have left.</summary>
    Task SendStatusAsync(PlayerData player);

    /// <summary>Tells the player how many free games they have left of a game.</summary>
    Task SendAccountStatusAsync(PlayerData player, int gameType);

    /// <summary>Tells the player the offers of game tokens.</summary>
    Task SendTokenOffersAsync(PlayerData player);

    /// <summary>Sells the player a token offer, and answers as a catalog purchase does.</summary>
    Task BuyTokensAsync(PlayerData player, int offerId);

    /// <summary>Puts the player in a game waiting for players, or a new one.</summary>
    Task QuickJoinAsync(PlayerData player);

    /// <summary>Starts a new game for the player on the arena of that name, or any arena.</summary>
    Task StartAsync(PlayerData player, string levelName);

    /// <summary>Takes the player out of the game they wait for.</summary>
    Task LeaveAsync(Guid playerId);

    /// <summary>The game the player waits for or plays in.</summary>
    [Orleans.Concurrency.AlwaysInterleave]
    Task<int?> GameOfAsync(Guid playerId);

    /// <summary>The game the player waits for or plays in, or else the last one they played, whose results may still
    /// be up.</summary>
    [Orleans.Concurrency.AlwaysInterleave]
    Task<int?> LastGameOfAsync(Guid playerId);

    /// <summary>A game ended; those who played in it are free again.</summary>
    Task GameOverAsync(int gameId, IReadOnlyList<Guid> players);

    /// <summary>A player walked out of a game before it ended: they are free of it, and wait a while to play again.</summary>
    Task QuitAsync(int gameId, Guid playerId);

    /// <summary>A game is over, or never went ahead: its arena is free for the next lobby waiting.</summary>
    Task GameEndedAsync(int gameId);

    /// <summary>A player wants to play again after a game: into a game waiting for players, with others who do too.</summary>
    Task RematchAsync(int previousGameId, PlayerData player);
}
