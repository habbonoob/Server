namespace Habbo.Silo.Abstractions.Furni;

/// <summary>One scoreboard row: a score and the names that share it, in board order, and when it was last
/// written, which a board that clears daily, weekly or monthly goes by.</summary>
[GenerateSerializer]
public sealed partial record FurniHighScoreEntryView(
    [property: Id(0)] int Score,
    [property: Id(1)] List<string> Users,
    [property: Id(2)] DateTimeOffset RecordedAt);
