namespace Habbo.Domain.Catalog;

/// <summary>Something a player may claim once for free, named by the id the client's special items window asks for.</summary>
public sealed class ProductClaim
{
    public const int IdMaxLength = 64;

    public string Id { get; set; } = string.Empty;

    /// <summary>Whether it can be claimed now.</summary>
    public bool Active { get; set; } = true;

    /// <summary>The furniture handed over, by class name.</summary>
    public List<string> Furni { get; set; } = [];

    /// <summary>What the player chooses one of, besides the furniture (the client sends the choice, as the cursed dragon
    /// adoption does: 0 blue, 1 purple); none when there is no choice.</summary>
    public List<ProductClaimChoice> Choices { get; set; } = [];
}

/// <summary>A choice of a claim: the furniture it hands over, by class name (the cursed dragon adoption's pet boxes), and the
/// badge that comes with it.</summary>
public sealed record ProductClaimChoice(string Furni, string Badge);
