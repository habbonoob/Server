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
}
