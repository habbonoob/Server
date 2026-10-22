namespace Habbo.Domain.Catalog;

/// <summary>An offer the hotel puts in front of players: a catalog offer at its own price, with its own
/// picture and words, bought up to a limit until it expires.</summary>
public sealed class TargetedOffer
{
    public int Id { get; set; }

    /// <summary>The name the client logs it by.</summary>
    public required string Identifier { get; set; }

    /// <summary>The catalog offer whose products it hands over.</summary>
    public int OfferId { get; set; }

    public int PriceInCredits { get; set; }

    public int PriceInActivityPoints { get; set; }

    public int ActivityPointType { get; set; }

    /// <summary>How many one player may buy; 0 for no limit.</summary>
    public int PurchaseLimit { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string IconImageUrl { get; set; } = string.Empty;

    /// <summary>The client's kind of offer.</summary>
    public int Type { get; set; }

    /// <summary>The product codes shown as its contents.</summary>
    public List<string> SubProductCodes { get; set; } = [];

    public int SortOrder { get; set; }
}
