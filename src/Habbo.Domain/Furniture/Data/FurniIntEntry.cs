namespace Habbo.Domain.Furniture.Data;

/// <summary>One element of an int-array item (type 5). Kept as a real integer column: the
/// client reads these as ints, and storing them as text would only invite a parse.</summary>
public sealed class FurniIntEntry : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int Position { get; set; }
    public int Value { get; set; }
}
