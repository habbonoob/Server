namespace Habbo.Domain.Furniture.Data;

/// <summary>The leading string of an item's data. Shared by the legacy, vote, high-score and
/// crackable layouts, which all begin with it.
///
/// For ordinary furniture this is the visual state the client renders — a dice showing 3, a
/// lamp that is on — and the client parses it as a number. It is stored as text because the
/// layouts that are not ordinary furniture put a non-numeric value here.</summary>
public sealed class FurniState : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public required string State { get; set; }
}
