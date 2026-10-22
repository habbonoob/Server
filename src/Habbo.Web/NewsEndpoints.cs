using Habbo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Web;

/// <summary>The hotel's news, for the website: the promo articles the hotel view shows, active ones in their order.
/// Anyone may read it; it is what a visitor sees before logging in.</summary>
public static class NewsEndpoints
{
    public static void MapNewsEndpoints(this WebApplication app) =>
        app.MapGet("/api/news", async (IDbContextFactory<AppDbContext> dbFactory, CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var articles = await db.PromoArticles.AsNoTracking().Where(a => a.Active).OrderBy(a => a.SortOrder)
                .Select(a => new NewsArticle(a.Id, a.Title, a.BodyText, a.ImageUrl))
                .ToListAsync(ct);
            return Results.Ok(articles);
        });
}
