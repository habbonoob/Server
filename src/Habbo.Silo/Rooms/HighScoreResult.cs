namespace Habbo.Silo.Rooms;

/// <summary>How one team did in a game that ended: its score, who played in it, and whether it won.</summary>
public sealed record HighScoreResult(int Score, IReadOnlyList<string> Users, bool Won);
