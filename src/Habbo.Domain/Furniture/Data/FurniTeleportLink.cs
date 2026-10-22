namespace Habbo.Domain.Furniture.Data;

/// <summary>One side of a teleport pair: stepping into <see cref="Item"/> comes out of
/// <see cref="LinkedItem"/>. Teleports are bought as a pair, so every link is stored from both
/// ends and each item finds its partner with one lookup on its own key.</summary>
public sealed class FurniTeleportLink : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int LinkedItemId { get; set; }
    public UserItem? LinkedItem { get; set; }
}
