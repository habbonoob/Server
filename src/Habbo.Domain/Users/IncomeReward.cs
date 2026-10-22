namespace Habbo.Domain.Users;

/// <summary>Earnings waiting for a player in their earnings window: credits or duckets in a category, or a product.
/// Claiming takes them.</summary>
public sealed class IncomeReward
{
    public const int ProductCodeMaxLength = 64;

    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The window's category, as the client numbers it.</summary>
    public byte Category { get; set; }

    /// <summary>Duckets or credits, as the client numbers them; a product's type is its own.</summary>
    public byte RewardType { get; set; }

    public int Amount { get; set; }

    /// <summary>The product given, by its productdata code; empty for credits or duckets.</summary>
    public string ProductCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
