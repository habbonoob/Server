namespace Habbo.Domain.Furniture.Data;

/// <summary>A crackable item (type 7) — an egg or a piñata. The client draws it further broken
/// as hits climbs toward the target, so both halves have to survive a restart.</summary>
public sealed class FurniCrackable : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int Hits { get; set; }
    public int Target { get; set; }
}
