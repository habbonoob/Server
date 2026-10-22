namespace Habbo.Domain.Furniture.Data;

/// <summary>A reward a give reward box gave a user: what it was, and when, for the box's limits.</summary>
public sealed class WiredRewardGrant
{
    public const int CodeMaxLength = 100;

    public long Id { get; set; }

    /// <summary>The box that gave it.</summary>
    public int WiredItemId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>A badge, or else a product.</summary>
    public bool IsBadge { get; set; }

    /// <summary>The badge's code or the product's.</summary>
    public string Code { get; set; } = string.Empty;

    public DateTimeOffset GivenAt { get; set; } = DateTimeOffset.UtcNow;
}
