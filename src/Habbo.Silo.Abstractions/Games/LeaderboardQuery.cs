using Orleans;

namespace Habbo.Silo.Abstractions.Games;

/// <summary>What part of a leaderboard the client asks for: the game, how many weeks back (for a weekly board), the
/// rank to start from (-1: the stretch around the player) and how many rows.</summary>
[GenerateSerializer]
public sealed partial record LeaderboardQuery(
    [property: Id(0)] int GameTypeId,
    [property: Id(1)] int WeekOffset,
    [property: Id(2)] int StartRank,
    [property: Id(3)] int WindowSize);
