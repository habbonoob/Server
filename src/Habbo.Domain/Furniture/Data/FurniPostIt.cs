namespace Habbo.Domain.Furniture.Data;

/// <summary>What is written on a note stuck to a wall. Its colour is the note's state.</summary>
public sealed class FurniPostIt : IFurniRow
{
    public const int MaxTextLength = 512;

    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public string Text { get; set; } = string.Empty;
}
