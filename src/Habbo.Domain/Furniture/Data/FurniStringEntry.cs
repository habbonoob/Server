namespace Habbo.Domain.Furniture.Data;

/// <summary>One element of a string-array item (type 2). The client reads the array in order,
/// so the position is part of the data rather than an implementation detail.</summary>
public sealed class FurniStringEntry : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int Position { get; set; }
    public required string Value { get; set; }
}
