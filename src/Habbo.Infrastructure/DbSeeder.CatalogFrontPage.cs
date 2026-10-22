using Habbo.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The catalog front page's promotions.</summary>
public static partial class DbSeeder
{
    private sealed record SeedCatalogFrontPage(List<SeedCatalogFrontPageItem>? Items);

    private sealed record SeedCatalogFrontPageItem(
        int Position, string Name, string PromoImage, CatalogFrontPageItemType Type, string? PageName, int OfferId,
        string? ProductCode, DateTimeOffset? ExpiresAt);

    /// <summary>seed/catalog_frontpage.json, seeded only while the table is empty, so a hotel's own promotions are kept.
    /// One opening a page the catalog does not have is left out: it would open nothing.</summary>
    private static async Task SeedCatalogFrontPageAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "catalog_frontpage.json";
        var seed = await seedFiles.ReadJsonAsync<SeedCatalogFrontPage>(file, ct);
        if (seed?.Items is not { Count: > 0 } items || await db.CatalogFrontPageItems.AnyAsync(ct))
            return;

        var pages = (await db.CatalogPages.Select(p => p.PageName).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var added = 0;
        foreach (var item in items)
        {
            if (item.Type == CatalogFrontPageItemType.Page && !pages.Contains(item.PageName ?? string.Empty))
            {
                logger.LogWarning("{File} promotes catalog page '{Page}', which does not exist.", file, item.PageName);
                continue;
            }

            db.CatalogFrontPageItems.Add(new CatalogFrontPageItem
            {
                Position = item.Position,
                Name = item.Name,
                PromoImage = item.PromoImage,
                Type = item.Type,
                PageName = item.PageName ?? string.Empty,
                OfferId = item.OfferId,
                ProductCode = item.ProductCode ?? string.Empty,
                ExpiresAt = item.ExpiresAt,
            });
            added++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} catalog front page item(s) from {File}.", added, file);
    }
}
