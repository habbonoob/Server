namespace Habbo.Domain.Marketplace;

/// <summary>How many offers a player may still put up: one token each.</summary>
public sealed class MarketplaceTokens
{
    public Guid UserId { get; set; }

    public int Tokens { get; set; }
}
