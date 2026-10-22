namespace Habbo.Domain.Furniture.Data;

/// <summary>One row of a scoreboard: a score and the players who share it. A row is a team
/// rather than a person, which is why the names hang off the entry.</summary>
public sealed class FurniHighScoreEntry
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public FurniHighScore? HighScore { get; set; }

    /// <summary>Position on the board, in the order the client shows them.</summary>
    public int Position { get; set; }
    public int Score { get; set; }

    /// <summary>When the row was last written; a board that clears daily, weekly or monthly drops older rows.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    public List<FurniHighScoreUser> Users { get; } = [];
}
