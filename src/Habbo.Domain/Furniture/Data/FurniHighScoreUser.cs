namespace Habbo.Domain.Furniture.Data;

/// <summary>One name on a scoreboard row. Stored as the name the client displays rather than
/// a player reference: a score outlives the account that set it.</summary>
public sealed class FurniHighScoreUser
{
    public int EntryId { get; set; }
    public FurniHighScoreEntry? Entry { get; set; }

    public int Position { get; set; }
    public required string Username { get; set; }
}
