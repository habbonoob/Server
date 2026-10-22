namespace Habbo.Domain.Furniture.Data;

/// <summary>What a trophy was engraved with when it was bought. The client wants it as one
/// tab-separated string; that is a wire format, so it is built on the way out rather than stored.</summary>
public sealed class FurniTrophy : IFurniRow
{
    public const int MaxEngraverNameLength = 32;
    public const int MaxMessageLength = 512;

    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    /// <summary>The name as engraved, not a player reference: an engraving does not change when
    /// the account is renamed or deleted.</summary>
    public required string EngraverName { get; set; }

    public DateTime EngravedAt { get; set; }
    public required string Message { get; set; }
}
