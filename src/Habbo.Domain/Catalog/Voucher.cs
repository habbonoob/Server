namespace Habbo.Domain.Catalog;

/// <summary>A voucher code and what it gives: credits, activity points, and an offer's products.
/// Each player redeems a code once; the code runs out after its uses or its date.</summary>
public sealed class Voucher
{
    public const int CodeMaxLength = 64;

    public required string Code { get; set; }

    public int Credits { get; set; }

    public int ActivityPoints { get; set; }

    public int ActivityPointType { get; set; }

    /// <summary>An offer whose products the voucher hands over, free.</summary>
    public int? OfferId { get; set; }

    /// <summary>How many times it may be redeemed in all; 0 for no limit.</summary>
    public int MaxUses { get; set; }

    public int Uses { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}
