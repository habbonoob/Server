namespace Habbo.Domain.Catalog;

/// <summary>A player redeeming a voucher code.</summary>
public sealed class VoucherRedemption
{
    public required string Code { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.UtcNow;
}
