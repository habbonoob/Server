using Habbo.Domain.Furniture;

namespace Habbo.Domain.Marketplace;

/// <summary>An item put up for sale on the marketplace. The item waits in escrow, out of its owner's
/// inventory, until someone buys it, the offer is cancelled, or it expires and its owner takes it back.
/// A sale's revenue waits for the seller to collect it.</summary>
public sealed class MarketplaceOffer
{
    public int Id { get; set; }

    public Guid SellerId { get; set; }

    /// <summary>The item on sale; null once a sold item is gone from the hotel (recycled, say), which leaves the sale
    /// and its revenue standing.</summary>
    public int? ItemId { get; set; }
    public UserItem? Item { get; set; }

    /// <summary>What kind of item it is, kept with the offer: its price history outlives the item.</summary>
    public int DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }

    /// <summary>What the buyer pays.</summary>
    public int Price { get; set; }

    /// <summary>What the seller receives: the price less the selling fee.</summary>
    public int Revenue { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public Guid? BuyerId { get; set; }

    public DateTimeOffset? SoldAt { get; set; }

    /// <summary>The seller collected the revenue, which takes the sale off their list (before September 2026, whose
    /// clients list a sale until it is cleared).</summary>
    public bool Redeemed { get; set; }

    /// <summary>The seller cleared the sale from their history; its revenue is theirs to collect all the same.</summary>
    public bool ClearedBySeller { get; set; }

    public bool IsSold => SoldAt is not null;

    public bool IsOpen(DateTimeOffset now) => !IsSold && ExpiresAt > now;
}
