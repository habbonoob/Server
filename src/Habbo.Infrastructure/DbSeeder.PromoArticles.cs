using Habbo.Domain.LandingView;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The landing view's promo articles.</summary>
public static partial class DbSeeder
{
    private sealed record SeedPromoArticles(List<SeedPromoArticle>? Articles);

    private sealed record SeedPromoArticle(
        int Id, string Title, string BodyText, string ButtonText, int LinkType, string LinkContent, string ImageUrl);

    /// <summary>seed/promo_articles.json, shown in the order listed. An article the hotel already has keeps
    /// whatever it was changed to.</summary>
    private static async Task SeedPromoArticlesAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var file = await seedFiles.ReadJsonAsync<SeedPromoArticles>("promo_articles.json", ct);
        var known = await db.PromoArticles.Select(a => a.Id).ToListAsync(ct);
        var order = 0;
        var added = 0;
        foreach (var seed in file?.Articles ?? [])
        {
            order++;
            if (known.Contains(seed.Id))
                continue;

            db.PromoArticles.Add(new PromoArticle
            {
                Id = seed.Id, Title = seed.Title, BodyText = seed.BodyText, ButtonText = seed.ButtonText, LinkType = seed.LinkType,
                LinkContent = seed.LinkContent, ImageUrl = seed.ImageUrl, SortOrder = order,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} promo article(s) from {File}.", added, "promo_articles.json");
        }
    }
}
