using Habbo.Domain.Bots;
using Habbo.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The kinds of bot the shop sells, and their offers.</summary>
public static partial class DbSeeder
{
    private sealed record SeedBots(string Page, List<SeedBotType>? Types);

    private sealed record SeedBotType(
        string Code, int OfferId, int PriceInCredits, string Figure, string DefaultName, string DefaultMotto, string Gender,
        List<int>? Skills, List<int>? Beverages, bool LogsVisitors);

    /// <summary>seed/bots.json. A kind is matched by its code and an offer by its id, so either keeps
    /// whatever the hotel changed it to; an offer that sells nothing yet gets its bot.</summary>
    private static async Task SeedBotsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var bots = await seedFiles.ReadJsonAsync<SeedBots>("bots.json", ct);
        if (bots is null)
            return;

        var known = await db.BotTypes.ToDictionaryAsync(t => t.Code, ct);
        foreach (var seed in (bots.Types ?? []).Where(t => !known.ContainsKey(t.Code)))
        {
            var type = new BotType
            {
                Code = seed.Code, DefaultName = seed.DefaultName, DefaultMotto = seed.DefaultMotto,
                Gender = Habbo.Domain.Users.GenderExtensions.ParseLetter(seed.Gender),
                Skills = seed.Skills ?? [], Beverages = seed.Beverages ?? [], LogsVisitors = seed.LogsVisitors,
            };
            db.BotTypes.Add(type);
            known[seed.Code] = type;
        }

        await db.SaveChangesAsync(ct);

        var pageId = await db.CatalogPages.Where(p => p.PageName == bots.Page).Select(p => (int?)p.Id).FirstOrDefaultAsync(ct);
        var added = 0;
        var position = 0;
        foreach (var seed in bots.Types ?? [])
        {
            var offer = await db.CatalogOffers.Include(o => o.Products).FirstOrDefaultAsync(o => o.Id == seed.OfferId, ct);
            if (offer is null)
            {
                if (pageId is not { } page)
                    continue;

                offer = new CatalogOffer
                {
                    Id = seed.OfferId, LocalizationId = "bot_" + seed.Code, PriceInCredits = seed.PriceInCredits, Giftable = true, MaxAmount = 1,
                };
                offer.Listings.Add(new CatalogListing { PageId = page, OrderIndex = position });
                db.CatalogOffers.Add(offer);
            }

            position++;
            if (offer.Products.Any(p => p.Type == CatalogProductType.Bot))
                continue;

            // an offer brought over from the live catalog came without its bot: the dump had no kind for it
            offer.Products.Add(new CatalogProduct
            {
                Type = CatalogProductType.Bot, ClassId = known[seed.Code].Id, ExtraParam = seed.Figure,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} bot offer(s) from {File}.", added, "bots.json");
        }
    }
}
