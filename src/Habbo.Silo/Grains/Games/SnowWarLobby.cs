using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Silo.Grains.Games;

/// <summary>A SnowStorm game waiting for players: its arena, who started it, who waits in it (in the order they came),
/// and its countdown once enough are waiting. A rematch lobby remembers the game it follows.</summary>
public sealed class SnowWarLobby(int gameId, int fieldType, string ownerName, int? rematchOf)
{
    /// <summary>SnowStorm, as the game center numbers its games.</summary>
    public const int SnowWarGameType = 0;

    public int GameId { get; } = gameId;
    public int FieldType { get; } = fieldType;
    public string OwnerName { get; } = ownerName;
    public int? RematchOf { get; } = rematchOf;
    public List<SnowWarPlayer> Players { get; } = [];
    public IGrainTimer? Countdown { get; set; }

    /// <summary>The team with fewer players, the first when they are level.</summary>
    public int NextTeam(int teams) =>
        Enumerable.Range(1, teams).OrderBy(team => Players.Count(p => p.Team == team)).ThenBy(team => team).First();

    public GameLobbyData ToWire(int maximumPlayers, int teams) => new(
        GameId, string.Empty, SnowWarGameType, FieldType, teams, maximumPlayers, OwnerName, 0,
        Players.Select(PlayerToWire).ToList());

    public static GameLobbyPlayerData PlayerToWire(SnowWarPlayer player) => new(
        player.AccountId, player.Name, player.Figure, player.Gender, player.Team, player.SkillLevel, player.TotalScore, player.ScoreToNextLevel);
}
