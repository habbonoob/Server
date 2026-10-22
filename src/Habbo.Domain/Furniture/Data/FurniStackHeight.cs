namespace Habbo.Domain.Furniture.Data;

/// <summary>The height a stack helper is set to, in hundredths of a tile, and whether avatars walk
/// on its tiles at that height.</summary>
public sealed class FurniStackHeight : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int Height { get; set; }

    public bool MultiWalk { get; set; }
}
