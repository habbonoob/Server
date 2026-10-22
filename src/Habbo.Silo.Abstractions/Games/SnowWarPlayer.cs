using Orleans;

namespace Habbo.Silo.Abstractions.Games;

/// <summary>A player in a SnowStorm game: who they are, their team, their skill as the game center shows it (the
/// level of their total score, the score, and what the next level needs), and the room they came from.</summary>
[GenerateSerializer]
public sealed partial record SnowWarPlayer(
    [property: Id(0)] Guid Id,
    [property: Id(1)] int AccountId,
    [property: Id(2)] string Name,
    [property: Id(3)] string Figure,
    [property: Id(4)] string Gender,
    [property: Id(5)] int Team,
    [property: Id(6)] int SkillLevel,
    [property: Id(7)] int TotalScore,
    [property: Id(8)] int ScoreToNextLevel,
    [property: Id(9)] int RoomBeforeGame);
