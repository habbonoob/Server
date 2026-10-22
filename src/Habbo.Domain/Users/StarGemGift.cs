namespace Habbo.Domain.Users;

/// <summary>A star gem one player gave another, kept so the daily allowance can be counted.</summary>
public sealed class StarGemGift
{
    public long Id { get; set; }

    public Guid GiverId { get; set; }

    public Guid ReceiverId { get; set; }

    public int Amount { get; set; }

    public DateTimeOffset GivenAt { get; set; } = DateTimeOffset.UtcNow;
}
