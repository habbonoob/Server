namespace Habbo.Domain.Furniture.Data;

/// <summary>A vote-counter item (type 3): its state plus the tally the client renders.</summary>
public sealed class FurniVoteResult : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public int Result { get; set; }
}
