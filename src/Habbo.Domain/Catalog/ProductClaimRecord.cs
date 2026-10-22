namespace Habbo.Domain.Catalog;

/// <summary>A player having claimed a free product.</summary>
public sealed class ProductClaimRecord
{
    public Guid UserId { get; set; }

    public string ClaimId { get; set; } = string.Empty;

    public DateTimeOffset ClaimedAt { get; set; } = DateTimeOffset.UtcNow;
}
