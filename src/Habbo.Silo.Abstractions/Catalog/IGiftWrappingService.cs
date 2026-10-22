namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>The gift wraps, with their furni resolved to the class ids the client names them by.</summary>
public interface IGiftWrappingService
{
    Task<GiftWrappingCatalog> GetAsync(CancellationToken ct = default);
}
