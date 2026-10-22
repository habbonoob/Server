using Habbo.Domain.Users;

namespace Habbo.Domain.Furniture.Data;

/// <summary>What a wrapped present holds — the offer it was bought as, or a single prize item — handed
/// over when it is opened, and who sent it. A limited edition keeps the number it was sold with.</summary>
public sealed class FurniGift : IFurniRow
{
    public const int MaxExtraDataLength = 256;

    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public Guid SenderId { get; set; }
    public User? Sender { get; set; }

    public int? OfferId { get; set; }

    /// <summary>A prize item, for a box that holds one rather than an offer.</summary>
    public int? PrizeDefinitionId { get; set; }

    /// <summary>What the buyer typed into the offer, e.g. a trophy's engraving.</summary>
    public string ExtraData { get; set; } = string.Empty;

    public int? LimitedSerial { get; set; }

    public int LimitedStack { get; set; }

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
