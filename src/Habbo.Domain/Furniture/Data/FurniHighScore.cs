namespace Habbo.Domain.Furniture.Data;

/// <summary>A scoreboard item (type 6). ScoreType and ClearType tell the client how the board
/// is scored and when it resets; the rows themselves are the entries.</summary>
public sealed class FurniHighScore : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int ScoreType { get; set; }
    public int ClearType { get; set; }

    public List<FurniHighScoreEntry> Entries { get; } = [];
}
