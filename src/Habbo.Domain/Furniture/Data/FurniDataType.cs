namespace Habbo.Domain.Furniture.Data;

/// <summary>How an item's data is laid out. The values are the client's own — it reads this
/// from the low byte of the stuff-data header and picks a parser from it, so they cannot be
/// renumbered.</summary>
public enum FurniDataType
{
    /// <summary>A single string, which for most furniture is its visual state as a number.</summary>
    Legacy = 0,
    Map = 1,
    StringArray = 2,
    VoteResult = 3,

    /// <summary>No payload at all.</summary>
    Empty = 4,
    IntArray = 5,
    HighScore = 6,
    Crackable = 7,
}
